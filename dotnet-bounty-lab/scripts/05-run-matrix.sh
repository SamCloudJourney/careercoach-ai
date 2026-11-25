#!/usr/bin/env bash
# Build the labs and run every probe suite on every in-scope runtime, then write
# labs/findings/MATRIX.md comparing verdicts across runtimes.
#
# Usage: scripts/05-run-matrix.sh [suite ...]      (default: all)
set -euo pipefail

LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=env.sh
source "$LAB_ROOT/scripts/env.sh" >/dev/null
LABS="$LAB_ROOT/labs"
LOGS="$LAB_ROOT/notes/logs"
mkdir -p "$LOGS" "$LABS/findings"
SUITES=("${@:-all}")
TFMS=(net10.0 net11.0)

echo "==> Building labs (Release, ${TFMS[*]})"
dotnet build "$LABS/BountyLab.slnx" -c Release -nologo -v q 2>&1 | grep -E 'error|Warn|Build succeeded' | sort -u || true

run_start="$(date -u +%Y%m%dT%H%M%SZ)"
for tfm in "${TFMS[@]}"; do
  echo
  echo "==> [$tfm] starting Lab.Target"
  (cd "$LABS/src/Lab.Target" && exec dotnet "bin/Release/$tfm/Lab.Target.dll") > "$LOGS/target-$tfm.log" 2>&1 &
  target_pid=$!
  trap 'kill $target_pid 2>/dev/null || true' EXIT
  set +e
  dotnet "$LABS/src/Lab.Probes/bin/Release/$tfm/labctl.dll" "${SUITES[@]}" 2>&1 | tee "$LOGS/probes-$tfm.log"
  rc=${PIPESTATUS[0]}
  set -e
  kill "$target_pid" 2>/dev/null || true
  wait "$target_pid" 2>/dev/null || true
  trap - EXIT
  echo "==> [$tfm] probes exit code $rc (2 = at least one FLAG)"
done

python3 -I - "$LABS/findings" "$run_start" "$LOGS" <<'PY'
import json, sys, glob, os, collections, re
root, since, logs = sys.argv[1], sys.argv[2], sys.argv[3]
rows = collections.OrderedDict()
runtimes = []
for f in sorted(glob.glob(os.path.join(root, "*.jsonl"))):
    stamp = f.rsplit("-", 1)[-1].split(".")[0]
    if stamp < since:
        continue
    for line in open(f):
        r = json.loads(line)
        rt = r["runtime"]
        if rt not in runtimes:
            runtimes.append(rt)
        rows.setdefault((r["Suite"], r["Id"]), {"title": r["Title"], "ref": r.get("Reference")})[rt] = r
icon = {"Pass": "✅", "Review": "🟡", "Flag": "🚩", "Error": "⚠️"}
out = ["# Probe matrix", "", f"Run: `{since}`  ·  runtimes: " + ", ".join(f"`{r}`" for r in runtimes), "",
       "✅ pass · 🟡 review · 🚩 flag · ⚠️ probe error · **Δ** = verdict differs between runtimes", "",
       "| Suite | Probe | " + " | ".join(runtimes) + " | Δ | Observed (" + (runtimes[-1] if runtimes else "") + ") |",
       "|---|---|" + "---|" * len(runtimes) + "---|---|"]
counts = collections.Counter()
for (suite, pid), d in rows.items():
    verdicts = [d.get(rt, {}).get("Verdict", "-") for rt in runtimes]
    for v in verdicts:
        counts[v] += 1
    delta = "**Δ**" if len(set(verdicts)) > 1 else ""
    obs = (d.get(runtimes[-1], {}).get("Observed", "") if runtimes else "").replace("|", "\\|").replace("\n", " ")[:120]
    out.append(f"| {suite} | `{pid}` {d['title'].replace('|', '/')} | " +
               " | ".join(icon.get(v, v) for v in verdicts) + f" | {delta} | {obs} |")
out[3] += "  ·  totals: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items()))
# Server-side exceptions: Kestrel surfacing a malformed body as BadHttpRequestException is the
# designed behaviour. Anything else (NullReference, IndexOutOfRange, ObjectDisposed, process
# crash...) is a robustness bug worth a closer look.
EXPECTED = re.compile(r"BadHttpRequestException|Bad chunk|ConnectionAbortedException|ConnectionResetException|OperationCanceledException|TaskCanceledException")
out += ["", "## Server-side exceptions", "", "| Runtime log | Exception (first line) | Count | Class |", "|---|---|---|---|"]
unexpected_total = 0
for log in sorted(glob.glob(os.path.join(logs, "target-*.log"))):
    lines = open(log, errors="replace").read().splitlines()
    seen = collections.Counter()
    for i, line in enumerate(lines):
        if line.startswith("fail:") or "Unhandled exception" in line:
            nxt = next((l.strip() for l in lines[i + 1:i + 4] if re.match(r"\s*(System|Microsoft)\.[\w.]+(Exception|Error)", l)), line.strip())
            seen[nxt[:160]] += 1
    for exc, n in seen.most_common():
        cls = "expected" if EXPECTED.search(exc) else "**UNEXPECTED**"
        unexpected_total += 0 if cls == "expected" else n
        out.append(f"| `{os.path.basename(log)}` | `{exc.replace('|', '/')}` | {n} | {cls} |")
if unexpected_total:
    print(f"!! {unexpected_total} unexpected server-side exception(s) - see MATRIX.md")
path = os.path.join(root, "MATRIX.md")
open(path, "w").write("\n".join(out) + "\n")
print(f"\n==> wrote {path}")
print("    totals:", dict(counts))
PY

#!/usr/bin/env bash
# Patch diffing: what changed between the previous release and each in-scope release.
# Servicing releases carry the month's security fixes, so every fixed bug is a seed for
# variant hunting (a bypass of a fix, or the same bug class elsewhere, is new and in scope).
#
# Writes notes/patch-diffs/<checkout>__<prev>..<tag>.md (summary, committed) and
#        notes/patch-diffs/raw/<...>.diff (full product diff, gitignored).
set -euo pipefail
LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec python3 -I - "$LAB_ROOT" <<'PY'
import json, os, re, subprocess, sys, collections
root = sys.argv[1]
m = json.load(open(os.path.join(root, "scope", "manifest.json")))
tiers = {k: v for k, v in m["component_tiers"].items() if not k.startswith("$")}
def tier_of(c):
    for t, v in tiers.items():
        if c in v.get("components", []): return t
    return "unclassified"
OUT = os.path.join(root, "notes", "patch-diffs"); os.makedirs(os.path.join(OUT, "raw"), exist_ok=True)
TEST = re.compile(r"(^|/)(tests?|testassets|TestAssets|benchmarks?|perf|samples?)/|\.Tests?/|Tests?\.cs$|/eng/|\.github/(?!workflows)|\.md$|\.xlf$|\.resx$|global\.json$|Versions\.(props|xml)$|Version\.Details\.(xml|props)$|source-manifest\.json$|NuGet\.config$", re.I)
SIGNAL = re.compile(r"\b(checked|overflow|bounds?|out of range|validat\w*|sanitiz\w*|normaliz\w*|travers\w*|escap\w*|limit\w*|max\w*length|too (large|long|many)|reject\w*|malformed|invalid|throw\w*|security|cve|msrc|unsafe|stackalloc|fixed\s*\(|Marshal\.|DangerousGetPinnableReference|Unsafe\.|ReadOnlySpan|SequenceEqual|ConstantTimeEquals|FixedTimeEquals|dispose\w*|use.?after|race|lock\s*\(|Interlocked)\b", re.I)

def git(repo, *args):
    return subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True, check=True).stdout

summary_rows = []
for c in m["checkouts"]:
    if c["kind"] != "product":
        continue
    prev, tag = c.get("previous_ref"), c["ref"]
    if c["ref_type"] == "commit":  # e.g. ASP.NET Core 2.3: diff the fetched commits via local refs
        prev, tag = ("refs/lab/previous" if prev else None), "refs/lab/current"
    repo = os.path.join(root, "src", c["dest"])
    if not prev or not os.path.isdir(os.path.join(repo, ".git")):
        continue
    is_vmr = c["dest"].startswith("vmr/")
    label_prev, label_tag = (c.get("previous_ref", "")[:12], c["ref"][:12]) if c["ref_type"] == "commit" else (prev, tag)
    name = f"{c['dest'].replace('/', '_')}__{label_prev}..{label_tag}"
    numstat = git(repo, "diff", "--numstat", "--no-renames", prev, tag)
    per_comp = collections.defaultdict(lambda: {"prod": 0, "test": 0, "add": 0, "del": 0, "files": []})
    for line in numstat.splitlines():
        a, d, path = line.split("\t", 2)
        comp = path.split("/")[1] if is_vmr and path.startswith("src/") and path.count("/") > 1 else (c.get("component", c["dest"].split("/")[-1]) if not is_vmr else "(vmr root)")
        a = int(a) if a.isdigit() else 0; d = int(d) if d.isdigit() else 0
        bucket = per_comp[comp]
        if TEST.search(path):
            bucket["test"] += 1
        else:
            bucket["prod"] += 1; bucket["add"] += a; bucket["del"] += d
            bucket["files"].append((a + d, a, d, path))
    prod = {f[3] for b in per_comp.values() for f in b["files"]}
    # Full diff once, then keep only product-file sections (avoids huge pathspec argument lists).
    full = git(repo, "diff", "--no-renames", "-U5", prev, tag)
    keep, chunks = False, []
    for line in full.splitlines(keepends=True):
        if line.startswith("diff --git "):
            keep = line.rstrip().split(" b/", 1)[-1] in prod
        if keep:
            chunks.append(line)
    raw = "".join(chunks)
    open(os.path.join(OUT, "raw", name + ".diff"), "w").write(raw)

    # security-signal lines among *added* product lines, grouped by file
    signals = collections.Counter(); current = None; examples = collections.defaultdict(list)
    for line in raw.splitlines():
        if line.startswith("+++ b/"):
            current = line[6:]
        elif line.startswith("+") and not line.startswith("+++") and current and SIGNAL.search(line):
            signals[current] += 1
            if len(examples[current]) < 3: examples[current].append(line[1:].strip()[:140])

    out = [f"# Patch diff: `{c['dest']}` {label_prev} → {label_tag}", "",
           f"Upstream `{c['repo']}`. Product files only (tests, eng, docs, version bumps excluded). "
           f"Full diff: `notes/patch-diffs/raw/{name}.diff` (regenerate with `scripts/08-patch-diff.sh`).", "",
           "| Component | Tier | Product files | +/- lines | Test files |", "|---|---|---|---|---|"]
    for comp, b in sorted(per_comp.items(), key=lambda kv: -(kv[1]["add"] + kv[1]["del"])):
        if b["prod"] == 0 and b["test"] == 0: continue
        out.append(f"| `{comp}` | {tier_of(comp)} | {b['prod']} | +{b['add']} / -{b['del']} | {b['test']} |")
    out += ["", "## Highest-churn product files in tier 1/2 components", "", "| +/- | File |", "|---|---|"]
    core = [f for comp, b in per_comp.items() if tier_of(comp) in ("tier1_core", "tier2_shipped_product") or not is_vmr for f in b["files"]]
    for tot, a, d, p in sorted(core, reverse=True)[:60]:
        out.append(f"| +{a}/-{d} | `{p}` |")
    out += ["", "## Security-signal hotspots (added lines matching validation / bounds / limits / unsafe vocabulary)", "",
            "A ranking for where to read first, not a verdict. Confirm each one by reading the hunk in the raw diff.", ""]
    for path, n in signals.most_common(40):
        out.append(f"- **{n}** `{path}`")
        for ex in examples[path]:
            out.append(f"  - `{ex.replace('`', '')}`")
    open(os.path.join(OUT, name + ".md"), "w").write("\n".join(out) + "\n")
    tp = sum(b["prod"] for b in per_comp.values())
    summary_rows.append((c["dest"], label_prev, label_tag, tp, len(signals), name))
    print(f"wrote notes/patch-diffs/{name}.md  ({tp} product files, {len(signals)} files with security signal)")

idx = ["# Patch diffs", "", "| Checkout | From | To | Product files changed | Files with security signal | Report |", "|---|---|---|---|---|---|"]
for dest, prev, tag, tp, ns, name in summary_rows:
    n = name + ".md"
    idx.append(f"| `{dest}` | `{prev}` | `{tag}` | {tp} | {ns} | [{n}]({n}) |")
open(os.path.join(OUT, "README.md"), "w").write("\n".join(idx) + "\n")
PY

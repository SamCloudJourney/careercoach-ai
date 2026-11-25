#!/usr/bin/env bash
# Audit the two surfaces the bounty names explicitly besides the runtime itself:
#   1. project templates shipped with each in-scope SDK  (instantiated fresh, then statically checked)
#   2. GitHub Actions workflows in the in-scope repositories (static lint for known workflow bug classes)
#
# Usage: scripts/09-template-audit.sh [--build]
# Output: notes/TEMPLATE-AUDIT.md, work dir labs/artifacts/templates/<sdk>/<template>
set -euo pipefail
LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$LAB_ROOT/scripts/env.sh" >/dev/null
BUILD="${1:-}"
WORK="$LAB_ROOT/labs/artifacts/templates"
rm -rf "$WORK"; mkdir -p "$WORK"

# Template short names + options that exercise security-relevant code paths.
TEMPLATES=(
  "web|"
  "webapi|"
  "webapi-auth|webapi --auth SingleOrg"
  "webapp|"
  "webapp-auth|webapp --auth Individual"
  "mvc-auth|mvc --auth Individual"
  "blazor|blazor"
  "blazor-auth|blazor --auth Individual --interactivity Server"
  "blazorwasm|blazorwasm"
  "grpc|grpc"
  "worker|worker"
  "razorclasslib|razorclasslib"
  "console|console"
)

for sdk in $(dotnet --list-sdks | awk '{print $1}'); do
  for entry in "${TEMPLATES[@]}"; do
    name="${entry%%|*}"; spec="${entry#*|}"; spec="${spec:-$name}"
    dir="$WORK/$sdk/$name"; mkdir -p "$dir"
    printf '{ "sdk": { "version": "%s", "rollForward": "disable" } }\n' "$sdk" > "$dir/global.json"
    ( cd "$dir" && dotnet new $spec -o app --no-restore > new.log 2>&1 ) || echo "  !! [$sdk] dotnet new $spec failed (see $dir/new.log)"
    if [[ "$BUILD" == "--build" && -d "$dir/app" ]]; then
      ( cd "$dir/app" && dotnet build -nologo -v q > ../build.log 2>&1 ) && echo "  built [$sdk] $name" || echo "  !! [$sdk] build failed: $name"
    fi
  done
  echo "==> instantiated templates with SDK $sdk"
done

exec python3 -I - "$LAB_ROOT" "$WORK" <<'PY'
import json, os, re, sys, glob, collections
root, work = sys.argv[1], sys.argv[2]

# ------------------------------------------------------------------ template checks
# Each check: id, description, file glob, regex, polarity ("bad" = finding if present, "need" = finding if absent)
CHECKS = [
    ("secret-literal", "Hard-coded secret / key / password literal", "**/*.json", r'"(?:Password|Secret|ApiKey|ClientSecret|SigningKey)"\s*:\s*"[^"]{6,}"', "bad"),
    ("conn-string-password", "Connection string with inline password", "**/*.json", r"Password=(?!;)[^;\"]+", "bad"),
    ("allowed-hosts-star", "AllowedHosts = * (host filtering disabled)", "**/appsettings.json", r'"AllowedHosts"\s*:\s*"\*"', "bad"),
    ("dev-exception-unguarded", "Developer exception page without environment check", "**/Program.cs", r"^(?!.*IsDevelopment).*UseDeveloperExceptionPage\(", "bad"),
    ("cors-any-origin", "CORS AllowAnyOrigin", "**/*.cs", r"AllowAnyOrigin\(\)", "bad"),
    ("cors-any-origin-credentials", "AllowAnyOrigin combined with AllowCredentials", "**/*.cs", r"AllowAnyOrigin\(\)[\s\S]{0,200}AllowCredentials\(\)", "bad"),
    ("insecure-cookie", "Cookie SecurePolicy = None / SameSite = None", "**/*.cs", r"CookieSecurePolicy\.None|SameSiteMode\.None", "bad"),
    ("disable-antiforgery", "Antiforgery disabled", "**/*.cs", r"DisableAntiforgery\(\)|IgnoreAntiforgeryToken", "bad"),
    ("raw-html", "Raw HTML rendering (MarkupString / Html.Raw)", "**/*.razor", r"\(MarkupString\)|Html\.Raw\(", "bad"),
    ("raw-html-cshtml", "Html.Raw in Razor views", "**/*.cshtml", r"Html\.Raw\(", "bad"),
    ("open-redirect", "Redirect to unvalidated returnUrl", "**/*.cs*", r"Redirect\(\s*returnUrl\s*\)", "bad"),
    ("http-url", "Plain http:// endpoint in launch/app settings", "**/launchSettings.json", r'"applicationUrl"\s*:\s*"http://[^;"]*"', "bad"),
    ("hsts", "HSTS enabled outside development (UseHsts)", "**/Program.cs", r"UseHsts\(\)", "need-web"),
    ("https-redirect", "HTTPS redirection (UseHttpsRedirection)", "**/Program.cs", r"UseHttpsRedirection\(\)", "need-web"),
    ("require-confirmed", "Identity requires confirmed account", "**/Program.cs", r"RequireConfirmedAccount\s*=\s*true", "need-auth"),
]
# Hits reviewed and judged intentional; reported separately so nothing is silently hidden.
BENIGN = {
    ("disable-antiforgery", "Pages/Error.cshtml.cs"): "Error page must render for requests that already failed (incl. antiforgery) validation",
}
WEB = {"web", "webapi", "webapi-auth", "webapp", "webapp-auth", "mvc-auth", "blazor", "blazor-auth", "grpc"}
AUTH = {"webapi-auth", "webapp-auth", "mvc-auth", "blazor-auth"}

results = collections.defaultdict(dict)  # (sdk, template) -> check -> evidence
sdks = sorted(os.listdir(work))
templates = []
for sdk in sdks:
    for t in sorted(os.listdir(os.path.join(work, sdk))):
        app = os.path.join(work, sdk, t, "app")
        if not os.path.isdir(app): continue
        if t not in templates: templates.append(t)
        for cid, desc, pattern, rx, pol in CHECKS:
            if pol == "need-web" and t not in WEB: continue
            if pol == "need-auth" and t not in AUTH: continue
            hits = []
            for f in glob.glob(os.path.join(app, pattern), recursive=True):
                if "/obj/" in f or "/bin/" in f or "/wwwroot/lib/" in f: continue
                text = open(f, errors="ignore").read()
                for mm in re.finditer(rx, text, re.M):
                    line = text[:mm.start()].count("\n") + 1
                    hits.append(f"{os.path.relpath(f, app)}:{line}")
            benign = [h for h in hits if (cid, h.rsplit(":", 1)[0]) in BENIGN]
            hits = [h for h in hits if h not in benign]
            if benign:
                results[(sdk, t)].setdefault("_benign", []).extend(f"{cid} {h}" for h in benign)
            if pol == "bad" and hits:
                results[(sdk, t)][cid] = hits
            elif pol.startswith("need") and not hits:
                results[(sdk, t)][cid] = ["absent"]

# ------------------------------------------------------------------ GitHub Actions lint
WF_RULES = [
    ("pr-target-checkout", "pull_request_target + checkout of PR head (pwn-request)", r"pull_request_target[\s\S]*?ref:\s*\$\{\{\s*github\.event\.pull_request\.head\.(sha|ref)"),
    ("expr-injection", "Untrusted github.event text interpolated into run:", r"run:[^\n]*\$\{\{\s*github\.event\.(issue|comment|pull_request|review|discussion|head_commit|workflow_run)\.[\w.]*(title|body|name|message|label|head_ref|ref)\b"),
    ("head-ref-injection", "github.head_ref interpolated into run:", r"run:[^\n]*\$\{\{\s*github\.head_ref"),
    ("write-all", "permissions: write-all", r"permissions:\s*write-all"),
    ("unpinned-action", "Third-party action pinned to a tag/branch, not a SHA", r"uses:\s*(?!actions/|github/|dotnet/|\./)[\w.-]+/[\w./-]+@(?![0-9a-f]{40}\b)[\w.-]+"),
    ("workflow-run-artifact", "workflow_run consuming artifacts (artifact poisoning)", r"workflow_run[\s\S]*download-artifact"),
]
wf_hits = collections.defaultdict(list)
wf_total = 0
for ver in ("net10.0", "net11.0"):
    for repo in ("aspnetcore", "runtime", "sdk", "templating", "winforms", "wpf", "windowsdesktop"):
        for f in sorted(glob.glob(os.path.join(root, "src", "vmr", ver, "src", repo, ".github", "workflows", "*.y*ml"))):
            wf_total += 1
            text = open(f, errors="ignore").read()
            for rid, desc, rx in WF_RULES:
                for mm in re.finditer(rx, text):
                    line = text[:mm.start()].count("\n") + 1
                    wf_hits[rid].append(f"{ver}/{repo}/.github/workflows/{os.path.basename(f)}:{line}")

# ------------------------------------------------------------------ report
out = ["# Template & GitHub Actions audit", "",
       "Generated by `scripts/09-template-audit.sh`. Templates are instantiated fresh from each in-scope SDK "
       "and checked statically; workflows are linted for known GitHub Actions bug classes.", "",
       "**How to read this.** A hit means \"look here\", not a vulnerability. Templates intentionally ship dev-only "
       "conveniences (e.g. `http://` launch profiles, `AllowedHosts: *`), and MSRC excludes samples that ignore security on purpose. "
       "A reportable template issue is an insecure default that survives into production use.", "",
       "## Templates", "",
       "| Check | " + " | ".join(f"`{t}`" for t in templates) + " |", "|---|" + "---|" * len(templates)]
for cid, desc, *_ in CHECKS:
    cells = []
    for t in templates:
        per = []
        for sdk in sdks:
            ev = results.get((sdk, t), {}).get(cid)
            if ev: per.append(f"{sdk.split('.')[0]}:{'absent' if ev == ['absent'] else len(ev)}")
        cells.append("⚠️ " + ", ".join(per) if per else "·")
    out.append(f"| {desc} | " + " | ".join(cells) + " |")
out += ["", "<details><summary>Evidence (file:line)</summary>", ""]
for (sdk, t), checks in sorted(results.items()):
    for cid, ev in checks.items():
        if cid == "_benign": continue
        out.append(f"- `{sdk}` `{t}` **{cid}**: " + ", ".join(f"`{e}`" for e in ev[:6]) + (" …" if len(ev) > 6 else ""))
out += ["", "</details>", "", "### Reviewed as intentional", ""]
for (cid, loc), why in BENIGN.items():
    n = sum(1 for checks in results.values() for b in checks.get("_benign", []) if b.startswith(cid + " " + loc))
    out.append(f"- **{cid}** at `{loc}` ({n} hits): {why}")
out += ["", "## GitHub Actions (in-scope repositories)", "",
        f"{wf_total} workflow files scanned across both in-scope VMRs.", "",
        "| Rule | Hits | Examples |", "|---|---|---|"]
for rid, desc, _ in WF_RULES:
    hits = wf_hits.get(rid, [])
    out.append(f"| {desc} | {len(hits)} | " + "<br>".join(f"`{h}`" for h in hits[:4]) + (" …" if len(hits) > 4 else "") + " |")
out += ["", "Workflows in the VMR are mirrored copies; confirm a finding against the live upstream repository "
        "(the commit is in `notes/COMPONENT-MAP.md`) before reporting, since mirrored workflows do not run in `dotnet/dotnet`.", ""]
path = os.path.join(root, "notes", "TEMPLATE-AUDIT.md")
open(path, "w").write("\n".join(out))
print(f"wrote {path}: {len(templates)} templates x {len(sdks)} SDKs, {wf_total} workflows")
PY

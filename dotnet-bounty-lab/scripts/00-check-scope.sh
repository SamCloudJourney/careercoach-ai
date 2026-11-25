#!/usr/bin/env bash
# Re-verify the bounty scope against live sources BEFORE you spend time on (or submit) a finding.
#
# Checks:
#   1. MSRC .NET bounty page: has it been revised since scope/manifest.json was written?
#   2. Microsoft release metadata: are the manifest's versions still the latest supported patches?
#      Has a release candidate of the next major appeared (RCs are in scope)? Has anything gone EOL?
#   3. dotnet/core bug-bounty-preview-scopes.md: any newly listed preview features?
#   4. The lab's own pins (LabEnvironment.cs, Directory.Build.props, checkouts) agree with the manifest.
#
# Exit 0 = manifest current; 1 = drift found (update scope/manifest.json, then re-run 01/02/03).
set -euo pipefail
LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec python3 -I - "$LAB_ROOT" <<'PY'
import datetime as dt, json, os, re, sys, urllib.request
root = sys.argv[1]
m = json.load(open(os.path.join(root, "scope", "manifest.json")))
p = m["program"]
drift, ok = [], []
def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": "dotnet-bounty-lab-scope-check"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return r.read().decode("utf-8", "replace")

# 1. MSRC page revision history
try:
    html = get(p["url"])
    months = "January|February|March|April|May|June|July|August|September|October|November|December"
    dates = [dt.datetime.strptime(d, "%B %d, %Y").date() for d in re.findall(rf"(?:{months}) \d{{1,2}}, 20\d\d", html)]
    latest = max(dates) if dates else None
    pinned = dt.date.fromisoformat(p["page_last_updated"])
    if latest and latest > pinned:
        drift.append(f"MSRC page revised {latest} (manifest pinned {pinned}) - re-read in/out of scope lists")
    else:
        ok.append(f"MSRC page latest revision {latest} matches manifest")
    for phrase in ("out of support versions", ".NET Framework", "Release candidates"):
        if phrase.lower() not in html.lower():
            drift.append(f"MSRC page no longer mentions '{phrase}' - scope wording changed")
except Exception as e:
    drift.append(f"could not fetch MSRC page: {e}")

# 2. release metadata
try:
    idx = json.loads(get(p["release_index_url"]))["releases-index"]
    pinned = {v["channel"]: v for v in m["versions"]["in_scope"]}
    today = dt.date.today()
    for r in idx:
        ch, phase = r["channel-version"], r["support-phase"]
        if phase in ("active", "maintenance"):
            if ch not in pinned:
                drift.append(f".NET {ch} is {phase} but not in manifest in_scope")
                continue
            v = pinned[ch]
            if r["latest-release"] != v["runtime"]:
                drift.append(f".NET {ch}: latest runtime is {r['latest-release']} (manifest {v['runtime']}) - repro must use the latest patch")
            else:
                # every SDK feature band shipped with the latest patch is in scope (e.g. 10.0.404 and 10.0.115)
                rel = json.loads(get(r["releases.json"]))["releases"][0]
                shipped = sorted(s["version"] for s in rel.get("sdks", []))
                pinned_sdks = sorted(v.get("sdks", [v["sdk"]]))
                if shipped != pinned_sdks:
                    drift.append(f".NET {ch}: release {rel['release-version']} ships SDKs {shipped} (manifest {pinned_sdks})")
                else:
                    ok.append(f".NET {ch} {phase}: runtime {v['runtime']} / SDKs {', '.join(shipped)} current (EOL {r.get('eol-date')})")
        elif phase in ("preview", "go-live"):
            if "-rc." in r["latest-release"]:
                drift.append(f".NET {ch} RC available ({r['latest-release']}): release candidates ARE in scope - add it")
            else:
                ok.append(f".NET {ch} is {phase} at {r['latest-release']} (previews are out of scope)")
        elif ch in pinned:
            drift.append(f".NET {ch} is now {phase} - remove from in_scope")
    for ch, v in pinned.items():
        if dt.date.fromisoformat(v["eol"]) <= today:
            drift.append(f".NET {ch} EOL date {v['eol']} has passed")
except Exception as e:
    drift.append(f"could not fetch release metadata: {e}")

# 2b. ASP.NET Core 2.3 on .NET Framework: latest 2.3.x on NuGet, and the commit it was built from
try:
    a = m["versions"]["aspnetcore_on_netfx"]
    pkg = a["nuget_probe"]
    vers = [x for x in json.loads(get(f"https://api.nuget.org/v3-flatcontainer/{pkg}/index.json"))["versions"]
            if re.fullmatch(r"2\.\d+\.\d+", x)]
    latest = max(vers, key=lambda x: tuple(map(int, x.split("."))))
    spec = get(f"https://api.nuget.org/v3-flatcontainer/{pkg}/{latest}/{pkg}.nuspec")
    commit = (re.search(r'commit="([0-9a-f]+)"', spec) or [None, None])[1]
    if latest != a["latest"] or commit != a["commit"]:
        drift.append(f"ASP.NET Core 2.x latest is {latest} @ {commit[:12] if commit else '?'} (manifest {a['latest']} @ {a['commit'][:12]})")
    else:
        ok.append(f"ASP.NET Core 2.x on .NET Framework: {latest} built from {commit[:12]} - matches manifest")
except Exception as e:
    drift.append(f"could not check ASP.NET Core 2.3 on NuGet: {e}")

# 3. preview feature list
try:
    md = get(p["preview_scope_url"].replace("github.com", "raw.githubusercontent.com").replace("/blob/", "/"))
    mm = re.search(r"Last Updated:\s*(\d{4}-\d{2}-\d{2})", md)
    stamps = sorted(re.findall(r"(\d{4}-\d{2}-\d{2})", md))
    newest = stamps[-1] if stamps else None
    if newest and newest > p["preview_scope_last_updated"]:
        drift.append(f"preview-scope page updated {newest} (manifest {p['preview_scope_last_updated']}) - check for new in-scope preview features")
    else:
        ok.append(f"preview-scope page unchanged since {p['preview_scope_last_updated']}")
except Exception as e:
    drift.append(f"could not fetch preview-scope page: {e}")

# 4. local pins
cs = open(os.path.join(root, "labs", "src", "Lab.Common", "LabEnvironment.cs")).read()
code = {int(k): f"{a}.{b}.{c}" for k, a, b, c in re.findall(r"\[(\d+)\] = new\((\d+), (\d+), (\d+)\)", cs)}
for v in m["versions"]["in_scope"]:
    major = int(v["channel"].split(".")[0])
    if code.get(major) != v["runtime"]:
        drift.append(f"LabEnvironment.cs pins .NET {major} = {code.get(major)} but manifest says {v['runtime']}")
props = open(os.path.join(root, "labs", "Directory.Build.props")).read()
for v in m["versions"]["in_scope"]:
    if f"<AspNetCorePackageVersion>{v['runtime']}</AspNetCorePackageVersion>" not in props:
        drift.append(f"labs/Directory.Build.props does not pin ASP.NET Core packages to {v['runtime']}")
for c in m["checkouts"]:
    prov = os.path.join(root, "src", c["dest"] + ".provenance")
    if not os.path.exists(prov):
        drift.append(f"checkout {c['dest']} missing - run scripts/02-fetch-source.sh {c['kind']}")
    elif f"ref={c['ref']}\n" not in open(prov).read():
        drift.append(f"checkout {c['dest']} is not at {c['ref']} - run scripts/02-fetch-source.sh {c['kind']}")
if not any("LabEnvironment" in d or "Directory.Build" in d or "checkout" in d for d in drift):
    ok.append(f"lab pins and all {len(m['checkouts'])} checkouts agree with manifest")

for line in ok: print(f"  ok    {line}")
for line in drift: print(f"  DRIFT {line}")
print("\nscope: " + ("CURRENT" if not drift else f"{len(drift)} issue(s) - update scope/manifest.json before relying on results"))
sys.exit(1 if drift else 0)
PY

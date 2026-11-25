#!/usr/bin/env bash
# Collect what Microsoft actually SHIPS for each in-scope release, next to the source:
#
#   templates   every template package bundled in the installed in-scope SDKs, extracted as shipped,
#               with the source repo + commit recorded in its nuspec   -> src/shipped/templates/
#   binaries    official release archives (runtime, ASP.NET Core, Windows Desktop, hosting bundle)
#               for win-x64 / linux-x64 / osx-arm64, SHA-512 verified    -> src/shipped/binaries/
#   aspnet23    every Microsoft.AspNetCore* package published at the latest 2.3.x (ASP.NET Core on .NET Framework)
#                                                                         -> src/shipped/aspnetcore-2.3/
#   cves        every CVE Microsoft lists in release metadata, all .NET channels -> scope/known-cves.json
#
# Usage: scripts/10-shipped-artifacts.sh [templates|binaries|aspnet23|cves|all]   (default all)
set -euo pipefail
LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec python3 -I - "$LAB_ROOT" "${1:-all}" <<'PY'
import hashlib, io, json, os, re, sys, urllib.request, zipfile, glob, shutil
root, what = sys.argv[1], sys.argv[2]
m = json.load(open(os.path.join(root, "scope", "manifest.json")))
SHIP = os.path.join(root, "src", "shipped")
META = "https://builds.dotnet.microsoft.com/dotnet/release-metadata"
UA = {"User-Agent": "dotnet-bounty-lab"}

def get(url, binary=False):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=120) as r:
        data = r.read()
    return data if binary else data.decode("utf-8", "replace")

def channel_release(ch, version):
    rel = json.loads(get(f"{META}/{ch}/releases.json"))
    return next(r for r in rel["releases"] if r["release-version"] == version)

# ------------------------------------------------------------------ templates
def templates():
    out_rows = []
    for pkg in sorted(glob.glob(os.path.join(root, ".dotnet", "templates", "*", "*.nupkg"))):
        runtime = os.path.basename(os.path.dirname(pkg))
        z = zipfile.ZipFile(pkg)
        spec = z.read(next(n for n in z.namelist() if n.endswith(".nuspec"))).decode("utf-8", "replace")
        pid = re.search(r"<id>([^<]+)</id>", spec).group(1)
        ver = re.search(r"<version>([^<]+)</version>", spec).group(1)
        repo = re.search(r'<repository[^>]*url="([^"]+)"[^>]*commit="([0-9a-f]+)"', spec)
        dest = os.path.join(SHIP, "templates", runtime, pid, ver)
        if os.path.isdir(dest): shutil.rmtree(dest)
        os.makedirs(dest)
        for n in z.namelist():
            if n.startswith(("_rels/", "package/")) or n == "[Content_Types].xml": continue
            target = os.path.realpath(os.path.join(dest, n))
            if not target.startswith(os.path.realpath(dest) + os.sep):  # never trust archive paths
                raise SystemExit(f"refusing path outside destination: {n}")
            os.makedirs(os.path.dirname(target), exist_ok=True)
            with open(target, "wb") as f: f.write(z.read(n))
        sha = hashlib.sha256(open(pkg, "rb").read()).hexdigest()
        prov = {"package": pid, "version": ver, "bundled_with_runtime": runtime, "nupkg_sha256": sha,
                "source_repo": repo.group(1) if repo else None, "source_commit": repo.group(2) if repo else None}
        json.dump(prov, open(dest + ".provenance.json", "w"), indent=2)
        n_templates = len(glob.glob(os.path.join(dest, "**", ".template.config", "template.json"), recursive=True))
        out_rows.append((runtime, pid, ver, n_templates, prov["source_commit"]))
        print(f"  template  {runtime:<8} {pid:<52} {ver:<34} {n_templates:>3} templates  src {prov['source_commit'][:12] if prov['source_commit'] else '?'}")
    return out_rows

# ------------------------------------------------------------------ binaries
def binaries():
    cfg = m["shipped_artifacts"]["binaries"]
    rids = set(cfg["rids"])
    for v in m["versions"]["in_scope"]:
        rel = channel_release(v["channel"], v["runtime"])
        dest = os.path.join(SHIP, "binaries", v["runtime"])
        os.makedirs(dest, exist_ok=True)
        wanted = []
        for product in ("runtime", "aspnetcore-runtime", "windowsdesktop"):
            for f in (rel.get(product) or {}).get("files", []):
                name = f["name"]
                if "hosting" in name and name.endswith(".exe"):
                    wanted.append(f)
                elif f.get("rid") in rids and "composite" not in name and (
                        name.endswith(".zip") if f["rid"].startswith("win") else name.endswith(".tar.gz")):
                    wanted.append(f)
        for f in wanted:
            fn = os.path.join(dest, os.path.basename(f["url"]))
            if not (os.path.exists(fn) and hashlib.sha512(open(fn, "rb").read()).hexdigest() == f["hash"].lower()):
                data = get(f["url"], binary=True)
                h = hashlib.sha512(data).hexdigest()
                if h != f["hash"].lower():
                    raise SystemExit(f"SHA-512 MISMATCH {f['url']}")
                open(fn, "wb").write(data)
            print(f"  binary    {v['runtime']:<8} {os.path.basename(fn):<60} {os.path.getsize(fn)/1e6:7.1f} MB  sha512 ok")
        json.dump([{k: f[k] for k in ("name", "rid", "url", "hash")} for f in wanted],
                  open(os.path.join(dest, "MANIFEST.json"), "w"), indent=2)

# ------------------------------------------------------------------ ASP.NET Core 2.3 packages
def aspnet23():
    a = m["versions"]["aspnetcore_on_netfx"]
    src = os.path.join(root, "src", "aspnetcore-2.3")
    if not os.path.isdir(src):
        print("  aspnet23: src/aspnetcore-2.3 not fetched yet - run scripts/02-fetch-source.sh product"); return
    ids = set()
    for proj in glob.glob(os.path.join(src, "src", "**", "src", "**", "*.csproj"), recursive=True):
        if re.search(r"/(test|tests|samples?|benchmarks?|testassets)/", proj, re.I): continue
        ids.add(os.path.splitext(os.path.basename(proj))[0])
    dest = os.path.join(SHIP, "aspnetcore-2.3", a["latest"]); os.makedirs(dest, exist_ok=True)
    got, missing = [], []
    for pid in sorted(ids):
        low = pid.lower()
        url = f"https://api.nuget.org/v3-flatcontainer/{low}/{a['latest']}/{low}.{a['latest']}.nupkg"
        fn = os.path.join(dest, f"{low}.{a['latest']}.nupkg")
        if not os.path.exists(fn):
            try:
                data = get(url, binary=True)
            except Exception:
                missing.append(pid); continue
            open(fn, "wb").write(data)
        z = zipfile.ZipFile(fn)
        spec = z.read(next(n for n in z.namelist() if n.endswith(".nuspec"))).decode("utf-8", "replace")
        commit = re.search(r'commit="([0-9a-f]+)"', spec)
        got.append({"package": pid, "version": a["latest"], "sha256": hashlib.sha256(open(fn, "rb").read()).hexdigest(),
                    "source_commit": commit.group(1) if commit else None})
    json.dump({"shipped": got, "not_published_at_this_version": missing}, open(os.path.join(dest, "MANIFEST.json"), "w"), indent=2)
    mismatched = [g["package"] for g in got if g["source_commit"] and g["source_commit"] != a["commit"]]
    print(f"  aspnet23  {len(got)} packages at {a['latest']} downloaded, {len(missing)} projects not published at that version, "
          f"{len(mismatched)} built from a different commit than {a['commit'][:12]}")

# ------------------------------------------------------------------ known CVEs
def cves():
    idx = json.loads(get(f"{META}/releases-index.json"))["releases-index"]
    known = {}
    for ch in idx:
        rel = json.loads(get(ch["releases.json"]))
        for r in rel["releases"]:
            for c in r.get("cve-list", []) or []:
                e = known.setdefault(c["cve-id"], {"cve": c["cve-id"], "url": c.get("cve-url"), "fixed_in": []})
                e["fixed_in"].append({"channel": ch["channel-version"], "release": r["release-version"], "date": r["release-date"]})
    for e in known.values():
        e["fixed_in"].sort(key=lambda x: x["date"])
        e["first_fixed"] = e["fixed_in"][0]["date"]
    data = sorted(known.values(), key=lambda e: e["first_fixed"], reverse=True)
    json.dump({"generated_from": META, "count": len(data), "cves": data},
              open(os.path.join(root, "scope", "known-cves.json"), "w"), indent=1)
    inscope = {v["channel"] for v in m["versions"]["in_scope"]}
    rows = ["# Known .NET CVEs", "",
            f"{len(data)} CVEs listed in Microsoft's release metadata across every .NET channel "
            "(generated by `scripts/10-shipped-artifacts.sh cves` into `scope/known-cves.json`).", "",
            "Anything here is **publicly known** and out of scope as a submission; use it to (1) rule out duplicates, "
            "(2) pick bug classes for variant hunting, and (3) find the fix commit in the patch diffs.", "",
            "| CVE | First fixed | Fixed in (in-scope channels) |", "|---|---|---|"]
    for e in data:
        fx = ", ".join(sorted({f"{f['release']}" for f in e["fixed_in"] if f["channel"] in inscope})) or "—"
        rows.append(f"| [{e['cve']}]({e['url']}) | {e['first_fixed']} | {fx} |")
    open(os.path.join(root, "notes", "KNOWN-CVES.md"), "w").write("\n".join(rows) + "\n")
    print(f"  cves      {len(data)} unique CVEs across {len(idx)} channels -> scope/known-cves.json, notes/KNOWN-CVES.md")

steps = {"templates": templates, "binaries": binaries, "aspnet23": aspnet23, "cves": cves}
for name in (steps if what == "all" else [what]):
    print(f"==> {name}")
    steps[name]()
PY

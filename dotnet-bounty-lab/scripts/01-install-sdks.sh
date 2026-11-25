#!/usr/bin/env bash
# Install the exact in-scope .NET SDKs (side by side) into ./.dotnet
# - versions come from scope/manifest.json
# - archives come from Microsoft's official release metadata
# - every archive is verified against the SHA-512 published in releases.json
#
# Usage: scripts/01-install-sdks.sh            # install all in-scope SDKs
#        source scripts/env.sh                 # put the lab SDKs on PATH
set -euo pipefail

LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MANIFEST="$LAB_ROOT/scope/manifest.json"
DOTNET_ROOT="$LAB_ROOT/.dotnet"
CACHE="$DOTNET_ROOT/.downloads"
META="https://builds.dotnet.microsoft.com/dotnet/release-metadata"
mkdir -p "$DOTNET_ROOT" "$CACHE"

case "$(uname -m)" in
  x86_64) ARCH=x64 ;; aarch64|arm64) ARCH=arm64 ;; *) echo "unsupported arch $(uname -m)"; exit 1 ;;
esac
case "$(uname -s)" in
  Linux) OS=linux ;; Darwin) OS=osx ;; *) echo "use the Windows installer on Windows"; exit 1 ;;
esac
RID="$OS-$ARCH"

python3 -I -c 'import json,sys
for v in json.load(open(sys.argv[1]))["versions"]["in_scope"]: print(v["channel"], v["sdk"])' "$MANIFEST" |
while read -r channel sdk; do
  if [[ -x "$DOTNET_ROOT/dotnet" ]] && "$DOTNET_ROOT/dotnet" --list-sdks 2>/dev/null | grep -q "^$sdk "; then
    echo "==> SDK $sdk already installed"; continue
  fi
  echo "==> Resolving SDK $sdk ($RID) from $META/$channel/releases.json"
  read -r url hash < <(curl -fsSL "$META/$channel/releases.json" | python3 -I -c '
import json, sys
sdk, rid = sys.argv[1], sys.argv[2]
d = json.load(sys.stdin)
for rel in d["releases"]:
    for s in [rel.get("sdk")] + rel.get("sdks", []):
        if s and s.get("version") == sdk:
            for f in s["files"]:
                if f.get("rid") == rid and f["name"].endswith(".tar.gz"):
                    print(f["url"], f["hash"]); sys.exit(0)
sys.exit("SDK %s for %s not found in release metadata" % (sdk, rid))' "$sdk" "$RID")
  file="$CACHE/$(basename "$url")"
  if [[ ! -f "$file" ]]; then
    curl -fL --retry 4 --retry-delay 3 -o "$file.part" "$url"
    mv "$file.part" "$file"
  fi
  echo "==> Verifying SHA-512"
  actual="$(sha512sum "$file" | awk '{print $1}')"
  if [[ "${actual,,}" != "${hash,,}" ]]; then
    echo "SHA-512 MISMATCH for $file" >&2; echo " expected $hash" >&2; echo " actual   $actual" >&2
    rm -f "$file"; exit 1
  fi
  echo "    ok $actual"
  tar -xzf "$file" -C "$DOTNET_ROOT"
  echo "==> Installed SDK $sdk"
done

"$DOTNET_ROOT/dotnet" --list-sdks
"$DOTNET_ROOT/dotnet" --list-runtimes
echo
echo "Run:  source $LAB_ROOT/scripts/env.sh"

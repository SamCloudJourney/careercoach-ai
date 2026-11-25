#!/usr/bin/env bash
# Fetch the in-scope .NET source at exact release tags.
#
# Layout produced (all under src/, which is gitignored):
#   src/vmr/net10.0      dotnet/dotnet (VMR) @ v10.0.15  (+ v10.0.14 fetched for patch diffing)
#   src/vmr/net11.0      dotnet/dotnet (VMR) @ v11.0.0   (+ v11.0.0-rc.3 fetched for patch diffing)
#   src/sdk-band/net10.0 dotnet/sdk + dotnet/templating @ v10.0.404 (latest 10.0 SDK feature band)
#
# Tags are read from scope/manifest.json so a single file controls what is "in scope".
# Idempotent: re-running updates the existing clones to the manifest tags.
set -euo pipefail

LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MANIFEST="$LAB_ROOT/scope/manifest.json"
SRC="$LAB_ROOT/src"
mkdir -p "$SRC"

need() { command -v "$1" >/dev/null || { echo "missing tool: $1" >&2; exit 1; }; }
need git; need python3

# Emits: <dest> <repo-url> <tag> <previous-tag-or-dash>
python3 -I - "$MANIFEST" <<'PY' > "$SRC/.fetch-plan"
import json, sys
m = json.load(open(sys.argv[1]))
for c in m["checkouts"]:
    print(c["dest"], c["repo"], c["tag"], c.get("previous_tag") or "-")
PY

clone_at_tag() {
  local dest="$1" url="$2" tag="$3" prev="$4"
  local dir="$SRC/$dest"
  if [[ -d "$dir/.git" ]]; then
    echo "==> [$dest] exists, syncing to $tag"
    git -C "$dir" fetch --depth 1 --no-tags origin "refs/tags/$tag:refs/tags/$tag"
    git -C "$dir" -c advice.detachedHead=false checkout -q "refs/tags/$tag"
  else
    echo "==> [$dest] cloning $url @ $tag"
    mkdir -p "$(dirname "$dir")"
    git -c advice.detachedHead=false clone --depth 1 --branch "$tag" --single-branch \
        --no-tags "$url" "$dir"
    git -C "$dir" tag -f "$tag" HEAD >/dev/null
  fi
  if [[ "$prev" != "-" ]]; then
    echo "==> [$dest] fetching previous release $prev for patch diffing"
    git -C "$dir" fetch --depth 1 --no-tags origin "refs/tags/$prev:refs/tags/$prev" || \
      echo "warn: could not fetch $prev" >&2
  fi
  # Record provenance next to the checkout.
  {
    echo "repo=$url"
    echo "tag=$tag"
    echo "commit=$(git -C "$dir" rev-parse HEAD)"
    echo "previous_tag=$prev"
    echo "fetched_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  } > "$dir.provenance"
}

while read -r dest url tag prev; do
  clone_at_tag "$dest" "$url" "$tag" "$prev"
done < "$SRC/.fetch-plan"

echo
echo "Done. Next: scripts/03-link-components.sh"
du -sh "$SRC"/*/* 2>/dev/null || true

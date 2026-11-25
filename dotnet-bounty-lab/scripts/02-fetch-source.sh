#!/usr/bin/env bash
# Fetch every in-scope source tree listed in scope/manifest.json ("checkouts").
#
# kinds:  product    shipped source at the exact release ref (+ previous release for patch diffing)
#         actions    live GitHub Actions: default branch, sparse + partial clone of .github only
#         docs       documentation and samples as published (live branch)
#         reference  release notes / CVE lists / preview-scope page
#
# Every checkout gets local refs  lab/current  and  lab/previous  plus  <dest>.provenance.
# Idempotent: re-running moves each checkout to the manifest ref.
#
# Usage: scripts/02-fetch-source.sh [kind]          JOBS=4 by default
set -euo pipefail

LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MANIFEST="$LAB_ROOT/scope/manifest.json"
SRC="$LAB_ROOT/src"
LOGS="$SRC/.logs"
JOBS="${JOBS:-4}"
ONLY="${1:-}"
mkdir -p "$SRC" "$LOGS"
command -v git >/dev/null && command -v python3 >/dev/null || { echo "need git and python3" >&2; exit 1; }

python3 -I - "$MANIFEST" "$ONLY" > "$SRC/.fetch-plan" <<'PY'
import json, sys
m = json.load(open(sys.argv[1])); only = sys.argv[2]
for c in m["checkouts"]:
    if only and c["kind"] != only:
        continue
    print("\t".join([c["dest"], c["repo"], c["ref"], c["ref_type"],
                     c.get("previous_ref") or "-", ",".join(c.get("sparse") or []) or "-"]))
PY

refspec() {
  case "$2" in
    tag)     echo "refs/tags/$1:refs/tags/$1" ;;
    branch)  echo "refs/heads/$1:refs/remotes/origin/$1" ;;
    commit)  echo "$1" ;;
    default) echo "HEAD" ;;
    *)       echo "unknown ref_type $2" >&2; return 1 ;;
  esac
}

fetch_one() {
  local dest="$1" url="$2" ref="$3" type="$4" prev="$5" sparse="$6"
  local dir="$SRC/$dest" log="$LOGS/${dest//\//_}.log"
  local filter=()
  [[ "$sparse" != "-" ]] && filter=(--filter=blob:none)
  {
    echo "== $dest  $url  $type:$ref  $(date -u +%FT%TZ)"
    if [[ ! -d "$dir/.git" ]]; then
      mkdir -p "$dir"
      git -C "$dir" init -q
      git -C "$dir" remote add origin "$url"
    else
      git -C "$dir" remote set-url origin "$url"
    fi
    if [[ "$sparse" != "-" ]]; then
      git -C "$dir" config remote.origin.promisor true
      git -C "$dir" config remote.origin.partialclonefilter blob:none
      git -C "$dir" sparse-checkout set --cone ${sparse//,/ }
    fi
    git -C "$dir" fetch --depth 1 --no-tags "${filter[@]}" origin "$(refspec "$ref" "$type")"
    git -C "$dir" -c advice.detachedHead=false checkout -q --force FETCH_HEAD
    git -C "$dir" update-ref refs/lab/current HEAD
    if [[ "$prev" != "-" ]]; then
      git -C "$dir" fetch --depth 1 --no-tags origin "$(refspec "$prev" "$type")"
      git -C "$dir" update-ref refs/lab/previous FETCH_HEAD
    fi
    {
      echo "repo=$url"
      echo "ref=$ref"
      echo "ref_type=$type"
      echo "commit=$(git -C "$dir" rev-parse HEAD)"
      echo "previous_ref=$prev"
      [[ "$prev" != "-" ]] && echo "previous_commit=$(git -C "$dir" rev-parse refs/lab/previous)"
      echo "sparse=$sparse"
      echo "fetched_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
    } > "$dir.provenance"
  } > "$log" 2>&1 && printf 'ok    %-40s %s\n' "$dest" "$(cut -c1-12 <<<"$(git -C "$dir" rev-parse HEAD)")" \
                   || printf 'FAIL  %-40s see %s\n' "$dest" "$log"
}

running=0
while IFS=$'\t' read -r dest url ref type prev sparse; do
  fetch_one "$dest" "$url" "$ref" "$type" "$prev" "$sparse" &
  if (( ++running >= JOBS )); then wait -n || true; running=$((running - 1)); fi
done < "$SRC/.fetch-plan"
wait

echo
grep -c . "$SRC/.fetch-plan" | xargs printf '%s checkouts in plan; '
grep -L '^commit=' $(sed 's|\t.*||; s|^|'"$SRC"'/|; s|$|.provenance|' "$SRC/.fetch-plan") 2>/dev/null | wc -l | xargs printf '%s missing provenance\n'
echo "Next: scripts/03-link-components.sh"

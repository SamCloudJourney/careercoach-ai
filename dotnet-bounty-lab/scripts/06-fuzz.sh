#!/usr/bin/env bash
# Smoke-fuzz every target on every in-scope runtime, replay every finding on every runtime,
# and write labs/findings/fuzz/FUZZ.md (which runtimes each input reproduces on).
#
# Usage: scripts/06-fuzz.sh [seconds-per-runtime=120] [target=all] [seed=1337]
set -euo pipefail
LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$LAB_ROOT/scripts/env.sh" >/dev/null
LABS="$LAB_ROOT/labs"; OUT="$LABS/findings/fuzz"
SECONDS_PER_RT="${1:-120}"; TARGET="${2:-all}"; SEED="${3:-1337}"
TFMS=(net10.0 net11.0)

dotnet build "$LABS/src/Lab.Fuzz" -c Release -nologo -v q 2>&1 | grep -E ' error |Build succeeded' | sort -u
for tfm in "${TFMS[@]}"; do
  echo "==> [$tfm] smoke $TARGET for ${SECONDS_PER_RT}s (seed $SEED)"
  dotnet "$LABS/src/Lab.Fuzz/bin/Release/$tfm/labfuzz.dll" smoke "$TARGET" --seconds "$SECONDS_PER_RT" --seed "$SEED" \
    --corpus "$LABS/fuzz-corpus" < /dev/null || true
done

echo "==> cross-runtime replay"
{
  echo "# Fuzz findings"
  echo
  echo "Each input is replayed on every in-scope runtime. A finding that reproduces only on the older"
  echo "runtime has usually been fixed in the newer one; check the source diff before spending time on it."
  echo
  echo "| Target | Kind | Input | ${TFMS[*]/%/ |} First line"
  echo "|---|---|---|$(printf '%s' "${TFMS[@]/*/---|}")---|"
  declare -A done_inputs=()
  for bin in "$OUT"/*/*/*.bin; do
    [[ -e "$bin" ]] || continue
    t="$(basename "$(dirname "$bin")")"; name="$(basename "$bin" .bin)"; kind="${name%%-*}"
    key="$t/$name"; [[ -n "${done_inputs[$key]:-}" ]] && continue; done_inputs[$key]=1
    row="| $t | $kind | \`${bin#$LABS/}\` |"
    first=""
    for tfm in "${TFMS[@]}"; do
      res="$(timeout 60 dotnet "$LABS/src/Lab.Fuzz/bin/Release/$tfm/labfuzz.dll" repro "$t" "$bin" < /dev/null 2>&1 || true)"
      if grep -q '^UNEXPECTED' <<<"$res"; then
        row+=" 🚩 repro |"; first="$(grep -A1 '^UNEXPECTED' <<<"$res" | tail -1 | cut -c1-140 | sed 's/|/\//g')"
      elif grep -q '^expected' <<<"$res"; then row+=" ✅ handled |"
      elif grep -q '^no exception' <<<"$res"; then row+=" ✅ ok |"
      else row+=" ⚠️ died/timeout |"; fi
    done
    echo "$row \`$first\` |"
  done
} > "$OUT/FUZZ.md"
echo "==> wrote $OUT/FUZZ.md"
cat "$OUT/FUZZ.md"

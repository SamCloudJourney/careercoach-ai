#!/usr/bin/env bash
# Coverage-guided fuzzing of a shipped .NET assembly with SharpFuzz + libFuzzer.
#
#  1. publishes labfuzz self-contained, so the framework assembly under test is a local copy
#  2. instruments that copy (e.g. System.Text.Json.dll) with SharpFuzz
#  3. runs it under libfuzzer-dotnet (built here with clang -fsanitize=fuzzer)
#
# Third-party tooling fetched by this script (review before first run):
#   - https://github.com/Metalnem/libfuzzer-dotnet   (C++ driver, compiled locally)
#   - NuGet SharpFuzz.CommandLine 2.3.0               (IL instrumenter, installed to tools/bin)
#
# Usage: scripts/07-libfuzzer.sh <target> [tfm=net11.0] [seconds=300] [-- extra libFuzzer flags]
set -euo pipefail
LAB_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$LAB_ROOT/scripts/env.sh" >/dev/null
TARGET="${1:?target name (labfuzz list)}"; TFM="${2:-net11.0}"; SECS="${3:-300}"
shift $(( $# < 3 ? $# : 3 )); [[ "${1:-}" == "--" ]] && shift
LABS="$LAB_ROOT/labs"; VENDOR="$LAB_ROOT/tools/vendor"; BIN="$LAB_ROOT/tools/bin"
PUB="$LABS/artifacts/libfuzzer-$TFM-$TARGET"
CORPUS="$LABS/fuzz-corpus/$TARGET"; FINDINGS="$LABS/findings/libfuzzer/$TFM/$TARGET"
RID="linux-$( [[ "$(uname -m)" == aarch64 ]] && echo arm64 || echo x64 )"
mkdir -p "$VENDOR" "$BIN" "$CORPUS" "$FINDINGS"

# --- driver
if [[ ! -x "$BIN/libfuzzer-dotnet" ]]; then
  echo "==> building libfuzzer-dotnet driver"
  [[ -d "$VENDOR/libfuzzer-dotnet" ]] || git clone --depth 1 https://github.com/Metalnem/libfuzzer-dotnet.git "$VENDOR/libfuzzer-dotnet"
  clang -O2 -fsanitize=fuzzer "$VENDOR/libfuzzer-dotnet/libfuzzer-dotnet.cc" -o "$BIN/libfuzzer-dotnet"
fi

# --- instrumenter (the tool targets an older runtime; let it roll forward to the lab's)
if [[ ! -x "$BIN/sharpfuzz" ]]; then
  echo "==> installing SharpFuzz.CommandLine"
  dotnet tool install SharpFuzz.CommandLine --version 2.3.0 --tool-path "$BIN" >/dev/null
fi

# --- publish + instrument a fresh copy every run (instrumentation is not idempotent)
echo "==> publishing labfuzz ($TFM, $RID, self-contained)"
rm -rf "$PUB"
dotnet publish "$LABS/src/Lab.Fuzz" -c Release -f "$TFM" -r "$RID" --self-contained \
  -p:PublishReadyToRun=false -o "$PUB" -nologo -v q >/dev/null
mapfile -t ASMS < <(dotnet "$LABS/src/Lab.Fuzz/bin/Release/$TFM/labfuzz.dll" assemblies "$TARGET")
[[ ${#ASMS[@]} -gt 0 ]] || { echo "target $TARGET lists no assemblies to instrument (CoreLib targets cannot be instrumented)"; exit 1; }
for a in "${ASMS[@]}"; do
  echo "==> instrumenting $a"
  DOTNET_ROLL_FORWARD=Major "$BIN/sharpfuzz" "$PUB/$a"
done

# --- seed corpus if empty
[[ -n "$(ls -A "$CORPUS")" ]] || printf 'seed' > "$CORPUS/seed-0"

echo "==> fuzzing $TARGET on $TFM for ${SECS}s  (crashes -> $FINDINGS)"
cd "$FINDINGS"
"$BIN/libfuzzer-dotnet" --target_path="$PUB/labfuzz" --target_arg="libfuzzer $TARGET" \
  -max_total_time="$SECS" -timeout=10 -rss_limit_mb=4096 -print_final_stats=1 "$@" "$CORPUS"

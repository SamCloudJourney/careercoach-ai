# Source me:  source scripts/env.sh
# Puts the lab's side-by-side in-scope SDKs first on PATH and keeps telemetry off.
_lab_root="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")/.." && pwd)"
export BOUNTY_LAB="$_lab_root"
export DOTNET_ROOT="$_lab_root/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$HOME/.dotnet/tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_MULTILEVEL_LOOKUP=0
# Labs pin RollForward=LatestPatch and print RuntimeInformation.FrameworkDescription,
# so every repro states the exact in-scope runtime it ran on.
unset _lab_root
echo "bounty lab: $BOUNTY_LAB  (dotnet $(dotnet --version 2>/dev/null || echo 'not installed - run scripts/01-install-sdks.sh'))"

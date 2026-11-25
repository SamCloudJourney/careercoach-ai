# Patch diff: `sdk-band/net10.0/sdk` v10.0.403 → v10.0.404

Upstream `https://github.com/dotnet/sdk.git`. Product files only (tests, eng, docs, version bumps excluded). Full diff: `notes/patch-diffs/raw/sdk-band_net10.0_sdk__v10.0.403..v10.0.404.diff` (regenerate with `scripts/08-patch-diff.sh`).

| Component | Tier | Product files | +/- lines | Test files |
|---|---|---|---|---|
| `sdk` | tier2_shipped_product | 12 | +939 / -136 | 8 |

## Highest-churn product files in tier 1/2 components

| +/- | File |
|---|---|
| +269/-113 | `eng/restore-toolset.ps1` |
| +198/-21 | `eng/restore-toolset.sh` |
| +128/-0 | `eng/dotnetup-shared.ps1` |
| +111/-0 | `eng/dotnetup-shared.sh` |
| +75/-1 | `eng/configure-toolset.sh` |
| +59/-0 | `eng/configure-toolset.ps1` |
| +42/-0 | `eng/sdk-tools.ps1` |
| +27/-0 | `src/Layout/redist/targets/OverlaySdkOnLKG.targets` |
| +25/-0 | `eng/sdk-tools.sh` |
| +3/-0 | `.gitignore` |
| +1/-1 | `src/Cli/dotnet/Installer/Windows/MsiPackageCache.cs` |
| +1/-0 | `src/Cli/dotnet/Commands/Workload/Install/NetSdkMsiInstallerClient.cs` |

## Security-signal hotspots (added lines matching validation / bounds / limits / unsafe vocabulary)

A ranking for where to read first, not a verdict. Confirm each one by reading the hunk in the raw diff.

- **4** `eng/dotnetup-shared.ps1`
  - `# parity. Throws on non-zero exit code.`
  - `if ($LASTEXITCODE -ne 0) { throw "$ErrorLabel exited with code $LASTEXITCODE." }`
  - `# $DotnetupDir. Throws on failure so callers can choose how to react.`
- **3** `eng/restore-toolset.ps1`
  - `# Set DOTNET_INSTALL_TEST_RUNTIMES=false to skip (e.g. cross-build containers with limited disk).`
  - `throw "Failed to install shared framework $version to '$dotNetRoot' using dotnet install script$architectureMessage (exit code '$installScri`
  - `throw "Installed a new SDK, deleting existing shared frameworks and sdk folders. Please rerun build"`
- **1** `eng/restore-toolset.sh`
  - `# Set DOTNET_INSTALL_TEST_RUNTIMES=false to skip (e.g. cross-build containers with limited disk).`
- **1** `src/Layout/redist/targets/OverlaySdkOnLKG.targets`
  - `<_CrossArchTestRuntimeSidecar>$([MSBuild]::NormalizeDirectory('$(ArtifactsDir)', 'test-runtimes', '$(TargetArchitecture)'))</_CrossArchTestR`

# Patch diff: `sdk-band/net10.0/templating` v10.0.403 → v10.0.404

Upstream `https://github.com/dotnet/templating.git`. Product files only (tests, eng, docs, version bumps excluded). Full diff: `notes/patch-diffs/raw/sdk-band_net10.0_templating__v10.0.403..v10.0.404.diff` (regenerate with `scripts/08-patch-diff.sh`).

| Component | Tier | Product files | +/- lines | Test files |
|---|---|---|---|---|
| `templating` | tier2_shipped_product | 5 | +106 / -6 | 4 |

## Highest-churn product files in tier 1/2 components

| +/- | File |
|---|---|
| +47/-2 | `eng/common/core-templates/job/helix-job-monitor.yml` |
| +42/-3 | `eng/common/core-templates/steps/send-to-helix.yml` |
| +9/-0 | `azure-pipelines.yml` |
| +6/-0 | `azure-pipelines-pr.yml` |
| +2/-1 | `eng/common/core-templates/job/job.yml` |

## Security-signal hotspots (added lines matching validation / bounds / limits / unsafe vocabulary)

A ranking for where to read first, not a verdict. Confirm each one by reading the hunk in the raw diff.

- **3** `eng/common/core-templates/job/helix-job-monitor.yml`
  - `- pwsh: throw "azureSubscription must be set when useEntraAuthentication is true."`
  - `displayName: Validate Helix Entra authentication`
  - `throw "The Helix Azure service connection did not provide a service principal or tenant ID."`
- **3** `eng/common/core-templates/steps/send-to-helix.yml`
  - `- pwsh: throw "HelixAzureSubscription must be set when HelixUseEntraAuthentication is true."`
  - `displayName: Validate Helix Entra authentication`
  - `throw "The Helix Azure service connection did not provide a service principal or tenant ID."`

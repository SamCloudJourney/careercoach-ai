# Scope: Microsoft .NET Bounty Program

> Verified **2026-11-25** against live sources by `scripts/00-check-scope.sh` (result: `CURRENT`).
> Machine-readable copy: [`scope/manifest.json`](scope/manifest.json). Re-run the check before relying on anything below.

## Sources

| Source | What it decides | Pinned revision |
|---|---|---|
| [MSRC .NET Bounty page](https://www.microsoft.com/en-us/msrc/bounty-dot-net-core) | Products, exclusions, awards | revision 2026-10-20 |
| [dotnet/core `bug-bounty-preview-scopes.md`](https://github.com/dotnet/core/blob/main/bug-bounty-preview-scopes.md) | Extra in-scope preview features | last updated 2021-11-15 (lists only .NET 6 HTTP/3, now GA) |
| [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) + [release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json) | Which versions are "current, supported" | fetched 2026-11-25 |

## In-scope versions (today)

| Channel | Type | Latest runtime | Latest SDK | Support ends | Source in this lab |
|---|---|---|---|---|---|
| **.NET 11** | STS | **11.0.0** | **11.0.100** | 2028-11-14 | `src/vmr/net11.0` @ `v11.0.0` |
| **.NET 10** | LTS | **10.0.15** | **10.0.404** | 2028-11-14 | `src/vmr/net10.0` @ `v10.0.15`, `src/sdk-band/net10.0` @ `v10.0.404` |

The program also covers "release candidates for upcoming versions". **No .NET 12 RC exists yet**; previews and nightly builds are explicitly excluded, so `main` branches are **not** downloaded.

## Out of scope (not downloaded)

| Excluded | Why (program wording) |
|---|---|
| .NET 9, .NET 8 | Reached end of support on 2026-11-10: "out of support versions" |
| .NET 7 and older | Out of support |
| .NET Framework, ASP.NET (WebForms/MVC) on .NET Framework | "Vulnerabilities in the .NET Framework…" (only the latest ASP.NET Core **2.x on .NET Framework** is covered; it is not part of this lab) |
| .NET 12 previews, daily builds, `main` | "daily builds, early beta releases, or are not RTM or RC versions" |
| IIS, OpenSSL, other platform tech | "platform technologies that are not unique to .NET" |
| Vendored third-party code | Listed under `out_of_scope_in_tree` in the manifest |

## What "the entire .NET" means here

Each in-scope release is downloaded as **`dotnet/dotnet`** (the "VMR"): the single repository Microsoft builds that release from, at the exact release tag. It holds every component that ships in the product, with each component's upstream commit recorded in `src/source-manifest.json` (summarised in [`notes/COMPONENT-MAP.md`](notes/COMPONENT-MAP.md)).

The program names products, not repositories, so this lab sorts components into tiers (lab terminology, not MSRC's):

| Tier | Components | Guidance |
|---|---|---|
| **1: core product** | `runtime` (CoreCLR, Mono, BCL, host), `aspnetcore` (Kestrel, Blazor, SignalR, Identity, MVC…) | Squarely in scope |
| **2: shipped product** | `sdk`, `templating`, `windowsdesktop`, `winforms`, `wpf` | In scope: SDK, templates (named explicitly), and the Windows Desktop runtime that ships as part of .NET |
| **3: toolchain** | `roslyn`, `msbuild`, `nuget-client`, `razor`, `fsharp`, `emsdk`, `efcore`, `diagnostics`, … | Ships with or alongside the SDK but is not named by the program. **Ask MSRC before investing.** |
| **Out (in tree)** | `arcade`, `scenario-tests`, `source-build-assets`, vendored natives | Build infrastructure / third-party |

Also explicitly in scope, and covered by `scripts/09-template-audit.sh`:
- **Templates** shipped with the current SDKs.
- **GitHub Actions** in the .NET / ASP.NET Core repositories.
- **Documentation and samples** on learn.microsoft.com for current versions (not downloaded; reportable when the guidance itself is insecure).

## Eligibility rules that shape every finding

- **Critical or Important** severity only. Award range USD 500–40,000. RCE/EoP top out at $40,000, SFB $30,000, remote DoS $20,000.
- Reproduce on the **latest patch** of an in-scope major. Every lab binary prints its runtime and refuses to call an older patch "in scope".
- Not publicly known. Fixes already visible in public source (e.g. hardening that landed in 11.0) may be treated as known.
- No "extensive or unlikely user actions", no reliance on **disabling built-in mitigations**, no low-impact CSRF, no server-side information disclosure.
- Test only systems you own. Every probe refuses non-loopback targets unless you pass `--i-own-this-target`.
- Submit through **https://aka.ms/secure-at** using [`reports/TEMPLATE.md`](reports/TEMPLATE.md).

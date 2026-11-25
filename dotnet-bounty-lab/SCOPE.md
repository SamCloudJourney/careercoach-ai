# Scope: Microsoft .NET Bounty Program

> Verified **2026-11-25** against live sources by `scripts/00-check-scope.sh` (result: `CURRENT`).
> Machine-readable copy: [`scope/manifest.json`](scope/manifest.json). Full holdings: [`notes/INVENTORY.md`](notes/INVENTORY.md).
> Re-run the check before relying on anything below.

## Sources

| Source | What it decides | Pinned revision |
|---|---|---|
| [MSRC .NET Bounty page](https://www.microsoft.com/en-us/msrc/bounty-dot-net-core) | Products, exclusions, awards | revision 2026-10-20 |
| [dotnet/core `bug-bounty-preview-scopes.md`](https://github.com/dotnet/core/blob/main/bug-bounty-preview-scopes.md) | Extra in-scope preview features | last updated 2021-11-15 (lists only .NET 6 HTTP/3, now GA) |
| [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) + [release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json) | Which versions are "current, supported"; which SDK bands shipped | fetched 2026-11-25 |
| NuGet (`Microsoft.AspNetCore.*` 2.x nuspecs) | Latest ASP.NET Core 2.x and the commit it was built from | 2.3.14 @ `094d253c4d4a` |

## Every in-scope item, and where it lives

Each line maps one sentence of the MSRC page to what the lab downloaded.

| MSRC wording | What that is today | In this lab |
|---|---|---|
| "Current, supported versions of Microsoft .NET and ASP.NET Core." | **.NET 10.0.15** (LTS, SDKs **10.0.115** + **10.0.404**) and **.NET 11.0.0** (STS, SDK **11.0.100**) | `src/vmr/net10.0` @ `v10.0.15` (= `v10.0.115`), `src/vmr/net10.0-sdk4xx` @ `v10.0.404`, `src/vmr/net11.0` @ `v11.0.0`; all three SDKs in `.dotnet/`; release binaries for win-x64 / linux-x64 / osx-arm64 incl. the IIS hosting bundle in `src/shipped/binaries/` |
| "The latest version of ASP.NET Core 2.X, if running on .NET Framework." | **ASP.NET Core 2.3.14** | `src/aspnetcore-2.3` @ `094d253c4d4a` (+ 2.3.13 `1db07eda0330` for diffing); all 104 published 2.3.14 packages in `src/shipped/aspnetcore-2.3/`, each verified as built from that commit |
| "Release candidates for upcoming versions of .NET." | **None.** No .NET 12 RC exists yet. | `00-check-scope.sh` flags one the moment it appears |
| ".NET and ASP.NET-Core templates provided with the current, supported versions" | **210 templates** in 16 packages bundled with the three SDKs | Extracted as shipped in `src/shipped/templates/` with nuspec provenance; their source is in the VMRs; instantiated and audited by `09-template-audit.sh` |
| "Any associated GitHub Actions in the associated .NET or ASP.NET Core GitHub repositories." | Workflows + composite actions on the **default branch** of the VMR and every component repo (where they actually run) | `src/github-actions/<repo>/.github` (23 repos, partial clone of `.github` only) |
| "Documentation on docs.microsoft.com or learn.microsoft.com/dotnet/, and samples in that documentation" | dotnet/docs, dotnet-api-docs, docs-desktop, AspNetCore.Docs (`live` = published), dotnet/samples, AspNetCore.Docs.Samples | `src/docs/` |
| "Any Preview Features listed in this GitHub page" | Only .NET 6 HTTP/3 (since GA) | `src/reference/dotnet-core` (also release notes and per-release CVE lists) |

## Out of scope (deliberately not downloaded)

| Excluded | Why (program wording) |
|---|---|
| .NET 9, .NET 8 | Reached end of support on 2026-11-10: "out of support versions" |
| .NET 7 and older | Out of support |
| .NET Framework itself; ASP.NET WebForms/MVC on .NET Framework | "Vulnerabilities in the .NET Framework, or any ASP.NET framework running on .NET Framework (Webforms or MVC)" |
| .NET 12 previews, daily builds, `main` branches of product code | "daily builds, early beta releases, or are not RTM or RC versions" (only their **GitHub Actions** are fetched from the default branch, because that is where Actions run) |
| IIS, OpenSSL, other platform tech | "platform technologies that are not unique to .NET" |
| Vendored third-party code | `out_of_scope_in_tree` in the manifest |
| .NET Aspire, .NET MAUI, dotnet/extensions, Orleans, YARP, ML.NET, EF Core 2.x, Docker images, Aspire/MAUI docs | Separate products, not named by the program |
| Microsoft.Extensions 8.0.x (a dependency of ASP.NET Core 2.3) | .NET 8 is out of support; coverage when consumed by ASP.NET Core 2.3 is unclear. **Ask MSRC.** |
| **Known CVEs** (262 listed in release metadata) | "Publicly-disclosed vulnerabilities which have already been reported…" Kept as data (`scope/known-cves.json`, `notes/KNOWN-CVES.md`) to rule out duplicates, not to re-report |

## Component tiers

The program names products, not repositories, so VMR components are sorted into tiers (lab terminology, not MSRC's).
Browse them under `src/by-tier/<tier>/<component>/{net10.0,net10.0-sdk4xx,net11.0,aspnetcore-2.3,github-actions-live}`.

| Tier | Components | Guidance |
|---|---|---|
| **1: core product** | `runtime` (CoreCLR, Mono, BCL, host), `aspnetcore` (Kestrel, Blazor, SignalR, Identity, MVC…, incl. 2.3) | Squarely in scope |
| **2: shipped product** | `sdk`, `templating`, `windowsdesktop`, `winforms`, `wpf`, docs | In scope: SDK, templates, Windows Desktop runtime, documentation |
| **3: toolchain** | `roslyn`, `msbuild`, `nuget-client`, `razor`, `fsharp`, `emsdk`, `efcore`, `diagnostics`, `vstest`, … | Ships with or alongside the SDK but is not named. **Ask MSRC before investing.** |
| **Out (in tree)** | `arcade`, `scenario-tests`, `source-build-assets`, vendored natives | Build infrastructure / third-party |

## Eligibility rules that shape every finding

- **Critical or Important** severity only. Award range USD 500–40,000. RCE/EoP top out at $40,000, SFB $30,000, remote DoS $20,000.
- Reproduce on the **latest patch** of an in-scope version. Every lab binary prints its runtime and refuses to call an older patch "in scope".
- Not publicly known: check `notes/KNOWN-CVES.md`. Fixes already visible in public source (e.g. hardening that landed in 11.0) may be treated as known.
- No "extensive or unlikely user actions", no reliance on **disabling built-in mitigations**, no low-impact CSRF, no server-side information disclosure.
- Test only systems you own. Every probe refuses non-loopback targets unless you pass `--i-own-this-target`.
- Keep results private (`notes/private/`, `labs/findings/`, both gitignored) until MSRC has triaged them.
- Submit through **https://aka.ms/secure-at** using [`reports/TEMPLATE.md`](reports/TEMPLATE.md).

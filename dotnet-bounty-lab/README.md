# .NET Bounty Lab

A security research lab for the **Microsoft .NET Bounty Program**: the full, in-scope .NET source at exact release
tags, side-by-side in-scope SDKs, and probe, fuzz, patch-diff and audit tooling that runs every test on **every in-scope
runtime** and shows where they differ.

> **Scope today (verified 2026-11-25):** .NET **10.0.15** (LTS, SDK 10.0.404) and .NET **11.0.0** (STS, SDK 11.0.100).
> .NET 8/9 went out of support on 2026-11-10; .NET Framework and previews are excluded. Details: [SCOPE.md](SCOPE.md).

## Quick start

```bash
make all                      # everything below, in order
# or step by step:
make scope                    # 00  re-verify scope against MSRC + release metadata (exit 1 on drift)
make sdks                     # 01  install SDK 10.0.404 + 11.0.100 into .dotnet/ (SHA-512 verified)
make source                   # 02  clone dotnet/dotnet (VMR) @ v10.0.15 and v11.0.0, + previous tags (~7.7 GB)
make map                      # 03  organise source by bounty tier -> notes/COMPONENT-MAP.md
make surface                  # 04  71-area attack-surface index -> notes/ATTACK-SURFACE.md
make matrix                   # 05  189 protocol/auth probes x 2 runtimes -> labs/findings/MATRIX.md
make fuzz                     # 06  21 parser fuzz targets x 2 runtimes + cross-replay -> labs/findings/fuzz/FUZZ.md
make diff                     # 08  patch diffs of each servicing release -> notes/patch-diffs/
make templates                # 09  template + GitHub Actions audit -> notes/TEMPLATE-AUDIT.md
source scripts/env.sh         # put the lab SDKs on PATH in your shell
```

Coverage-guided fuzzing (`make libfuzzer TARGET=json-document FUZZ_SECONDS=300`) is ready but **opt-in**. It downloads and compiles
third-party tooling (libfuzzer-dotnet, SharpFuzz.CommandLine), so review `scripts/07-libfuzzer.sh` first.

## Layout

```
dotnet-bounty-lab/
├── SCOPE.md                 what is in / out of scope, with sources
├── scope/
│   ├── manifest.json        single source of truth: versions, tags, checkouts, component tiers
│   └── hotspots.tsv         attack-surface areas -> paths, bug classes, lab coverage, public history
├── scripts/                 numbered pipeline (00-09) + env.sh
├── src/                     (gitignored, rebuilt by `make source map`)
│   ├── vmr/net10.0          dotnet/dotnet @ v10.0.15   (+ v10.0.14 for diffing)
│   ├── vmr/net11.0          dotnet/dotnet @ v11.0.0    (+ v11.0.0-rc.3 for diffing)
│   ├── sdk-band/net10.0     dotnet/sdk + dotnet/templating @ v10.0.404
│   └── by-tier/             symlinks: tier1_core/{runtime,aspnetcore}/{net10.0,net11.0}, tier2_..., tier3_..., out_of_scope_in_tree/
├── labs/                    BountyLab.slnx - every project builds for net10.0 AND net11.0
│   ├── src/Lab.Common       runtime banner + in-scope check, loopback guard, raw sockets, reporting
│   ├── src/Lab.Target       ASP.NET Core app built from in-box defaults only (HTTP/1.1, h2c, auth, SignalR)
│   ├── src/Lab.Probes       `labctl`: http1 (46) · http2 (36) · auth (98) · signalr (9) probes
│   └── src/Lab.Fuzz         `labfuzz`: 21 targets with oracles (round-trip, differential, safety invariants)
├── notes/
│   ├── private/             (gitignored) unreported leads, repro inputs, result snapshots - never publish before MSRC triage
│   ├── COMPONENT-MAP.md     every VMR component, tier, upstream repo + commit per version
│   ├── ATTACK-SURFACE.md    where to hunt, how big, how much changed 10->11, what the lab covers
│   ├── patch-diffs/         servicing-release diffs ranked by security signal (raw diffs gitignored)
│   └── TEMPLATE-AUDIT.md    13 templates x 2 SDKs, 145 workflows
└── reports/TEMPLATE.md      MSRC "complete report" skeleton with a pre-submit checklist
```

## How the pieces fit

1. **Scope first.** `00-check-scope.sh` compares `scope/manifest.json` with the live MSRC page, Microsoft's release
   metadata and the preview-feature list. Every lab binary prints `IN SCOPE` / `CHECK SCOPE` for the runtime it is running on.
2. **Differential by default.** Probes and fuzz targets run on both runtimes, and the reports mark **Δ** where
   10.0 and 11.0 behave differently. A Δ usually means the newer code hardened something. Trace it to the
   exact source lines in `src/vmr/net10.0` vs `src/vmr/net11.0` before deciding whether it matters.
3. **Oracles, not just crashes.** The fuzz targets check properties: `Uri` canonicalisation is idempotent, the
   backtracking and NonBacktracking regex engines agree, encoders never emit raw metacharacters, archive
   extraction stays inside its destination, segmented and contiguous JSON readers agree. Each target names the VMR
   path of the code under test.
4. **Patch diffs feed variant hunting.** `08-patch-diff.sh` ranks what each servicing release changed. A bypass of a
   fix, or the same bug elsewhere, is new and in scope.
5. **Report.** Reproduce on the latest patch, fill in [reports/TEMPLATE.md](reports/TEMPLATE.md), submit at https://aka.ms/secure-at.

## Handling results

Probe and fuzz output lands in `labs/findings/` and research notes go in `notes/private/`. Both are gitignored on
purpose: publishing an unreported issue counts as public disclosure and makes it ineligible
("publicly-disclosed vulnerabilities" are out of scope). Keep them local or in a private store until MSRC has triaged them.

## Rules of engagement

- Probes refuse non-loopback hosts unless `--i-own-this-target` is passed. Test only systems you own.
- Nothing in `Lab.Target` weakens a default: reports that depend on disabled mitigations are out of scope.
- Keep potential security issues private until MSRC has triaged them (Coordinated Vulnerability Disclosure).

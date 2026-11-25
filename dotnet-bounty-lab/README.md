# .NET Bounty Lab

A security research lab for the **Microsoft .NET Bounty Program**. It holds **everything the program covers**: the
shipped source of every in-scope release at its exact ref, the templates, binaries and packages as shipped, the live
GitHub Actions, the documentation and samples, and the list of already-known CVEs. It adds probe, fuzz, patch-diff and audit
tooling that runs every test on **every in-scope runtime** and shows where they differ.

> **Scope today (verified 2026-11-25):** .NET **10.0.15** (LTS; SDKs 10.0.115 + 10.0.404), .NET **11.0.0** (STS; SDK 11.0.100),
> and **ASP.NET Core 2.3.14** on .NET Framework. .NET 8/9 went out of support on 2026-11-10; .NET Framework itself and previews
> are excluded. Item-by-item mapping: [SCOPE.md](SCOPE.md). Everything held: [notes/INVENTORY.md](notes/INVENTORY.md).

## Quick start

```bash
make all                      # everything below, in order
# or step by step:
make scope                    # 00  re-verify scope against MSRC + release metadata (exit 1 on drift)
make sdks                     # 01  install SDKs 10.0.115 + 10.0.404 + 11.0.100 into .dotnet/ (SHA-512 verified)
make source                   # 02  fetch all 34 in-scope checkouts: product, ASP.NET Core 2.3, live Actions, docs (~10 GB)
make shipped                  # 10  shipped templates, release binaries, 2.3 packages, known-CVE registry
make map                      # 03  organise by bounty tier -> notes/COMPONENT-MAP.md + notes/INVENTORY.md
make surface                  # 04  71-area attack-surface index -> notes/ATTACK-SURFACE.md
make matrix                   # 05  189 protocol/auth probes x 2 runtimes -> labs/findings/MATRIX.md
make fuzz                     # 06  21 parser fuzz targets x 2 runtimes + cross-replay -> labs/findings/fuzz/FUZZ.md
make diff                     # 08  patch diffs of each servicing release -> notes/patch-diffs/
make templates                # 09  template + live GitHub Actions audit -> notes/private/TEMPLATE-AUDIT.md
source scripts/env.sh         # put the lab SDKs on PATH in your shell
```

Coverage-guided fuzzing (`make libfuzzer TARGET=json-document FUZZ_SECONDS=300`) is ready but **opt-in**. It downloads and compiles
third-party tooling (libfuzzer-dotnet, SharpFuzz.CommandLine), so review `scripts/07-libfuzzer.sh` first.

## Layout

```
dotnet-bounty-lab/
├── SCOPE.md                 each MSRC scope sentence -> what it means today -> where it is in this lab
├── scope/
│   ├── manifest.json        single source of truth: versions, SDK bands, 34 checkouts, shipped artifacts, tiers
│   ├── known-cves.json      262 CVEs from release metadata (publicly known = out of scope)
│   └── hotspots.tsv         attack-surface areas -> paths, bug classes, lab coverage, public history
├── scripts/                 numbered pipeline (00-10) + env.sh
├── src/                     (gitignored, rebuilt by `make source shipped map`)
│   ├── vmr/net10.0          dotnet/dotnet @ v10.0.15 = SDK 10.0.115   (+ v10.0.14)
│   ├── vmr/net10.0-sdk4xx   dotnet/dotnet @ v10.0.404 (SDK 4xx band)  (+ v10.0.403)
│   ├── vmr/net11.0          dotnet/dotnet @ v11.0.0 = SDK 11.0.100    (+ v11.0.0-rc.3)
│   ├── aspnetcore-2.3       dotnet/aspnetcore @ 2.3.14 commit         (+ 2.3.13)
│   ├── github-actions/      live .github of the VMR + 22 component repos (default branch)
│   ├── docs/                dotnet/docs, dotnet-api-docs, docs-desktop, AspNetCore.Docs (live), samples, AspNetCore.Docs.Samples
│   ├── reference/           dotnet/core (release notes, CVE lists, preview-scope page)
│   ├── shipped/             templates (210, as shipped) · binaries (win/linux/osx, hosting bundle) · aspnetcore-2.3 packages (104)
│   └── by-tier/             one tree: <tier>/<component>/{net10.0,net10.0-sdk4xx,net11.0,aspnetcore-2.3,github-actions-live}
├── labs/                    BountyLab.slnx - every project builds for net10.0 AND net11.0
│   ├── src/Lab.Common       runtime banner + in-scope check, loopback guard, raw sockets, reporting
│   ├── src/Lab.Target       ASP.NET Core app built from in-box defaults only (HTTP/1.1, h2c, auth, SignalR)
│   ├── src/Lab.Probes       `labctl`: http1 (46) · http2 (36) · auth (98) · signalr (9) probes
│   └── src/Lab.Fuzz         `labfuzz`: 21 targets with oracles (round-trip, differential, safety invariants)
├── notes/
│   ├── INVENTORY.md         everything held: path, repo, ref, commit, diff base, size, scope basis
│   ├── COMPONENT-MAP.md     every VMR component, tier, upstream repo + commit per release
│   ├── ATTACK-SURFACE.md    where to hunt, how big, how much changed 10->11, 2.3 equivalents, what the lab covers
│   ├── KNOWN-CVES.md        publicly known issues (rule out duplicates, pick variant classes)
│   ├── patch-diffs/         4 servicing diffs incl. ASP.NET Core 2.3.13->2.3.14, ranked by security signal
│   └── private/             (gitignored) leads, repro inputs, probe/fuzz/audit results - never publish before MSRC triage
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

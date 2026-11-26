# MSRC report: <one-line title: component, bug class, impact>

<!--
  Submit through the MSRC Researcher Portal: https://aka.ms/secure-at
  MSRC grades complete reports higher: https://aka.ms/complete-report
  Before submitting:
    [ ] scripts/00-check-scope.sh exits 0 (versions + scope wording current)
    [ ] reproduced on the LATEST patch of every affected in-scope major (banner says IN SCOPE)
    [ ] not already public (dotnet/announcements, GitHub issues/PRs, MSRC Security Update Guide)
    [ ] works with default settings; does not rely on disabling a built-in mitigation
    [ ] impact is in .NET / ASP.NET Core itself, not IIS/OpenSSL/a third-party package
    [ ] tested only against your own lab instances (MSRC Rules of Engagement)
-->

## Summary

<2-4 sentences: what is wrong, where, and what an attacker gains.>

## Affected products and versions

| Product | Version tested | Result |
|---|---|---|
| .NET runtime / ASP.NET Core 10.0 (LTS) | 10.0.15 (SDK 10.0.404) | vulnerable / not vulnerable |
| .NET runtime / ASP.NET Core 11.0 (STS) | 11.0.0 (SDK 11.0.100) | vulnerable / not vulnerable |

OS / architecture: <e.g. Ubuntu 24.04 x64; Windows 11 x64>
Hosting model: <Kestrel direct / behind proxy X / IIS in-process / HTTP.sys>

## Vulnerability type and impact

- **Bounty category:** <Remote Code Execution / Elevation of Privilege / Security Feature Bypass / Remote Denial of Service / Spoofing or Tampering / Information Disclosure / Insecure documentation or samples>
- **Proposed severity:** <Critical / Important> (Microsoft's severity guidance)
- **CWE:** <CWE-444 request smuggling, CWE-863 incorrect authorization, ...>
- **Preconditions:** <authentication needed? configuration? network position? user interaction?>
- **Impact:** <what the attacker can concretely do, against whom>

## Root cause

<File and line in the shipped source: `src/vmr/net10.0/src/<component>/...:<line>` at VMR tag `v10.0.15`
(commit from notes/COMPONENT-MAP.md). Explain why the code is wrong, quoting the minimal snippet.
If 11.0 differs, include both and explain the difference.>

## Steps to reproduce

1. Install the exact SDK: `scripts/01-install-sdks.sh` (or <SDK download link>).
2. Create the minimal app:

   ```csharp
   // Program.cs - default template + only what is needed to reach the bug
   ```

3. Run it: `dotnet run -f net10.0`
4. Send the trigger:

   ```bash
   # exact bytes; use printf/ncat for raw protocol bugs
   ```

5. Observe: <expected vs actual output, with the relevant log lines>

## Proof of concept

<Attach a zip containing the minimal project + trigger script; no extra dependencies.
State the exact expected output so triage can confirm in minutes.>

## Suggested fix

<Optional but valued: the smallest change that fixes the root cause, and why it is sufficient.>

## Variant analysis

<Did you check sibling code paths? (e.g. HTTP/1.1 vs HTTP/2 vs HTTP/3, sync vs async, Kestrel vs HTTP.sys).
List what you checked and the result.>

## Disclosure

I have not disclosed this issue publicly and will follow Coordinated Vulnerability Disclosure.
Test activity was limited to systems I own.

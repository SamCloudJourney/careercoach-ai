# Working rules for this repository

## Subagents: no more than 2-3 at a time

- Never run more than **3** subagents concurrently. Default to **2**; use a third only when the work is clearly independent.
- This covers every way of spawning agents: the Agent tool, forks, workflows, and remote sessions.
- If a task wants more parallelism, queue the rest and start the next agent only when one finishes.
- Workflow scripts must cap their fan-out at 3 concurrent agents, whatever the default size guideline says.
- Prefer doing the work directly when it is a handful of tool calls.

## dotnet-bounty-lab

- Start with `dotnet-bounty-lab/README.md` and `SCOPE.md`. `scope/manifest.json` is the single source of truth for what is in scope.
- Run `dotnet-bounty-lab/scripts/00-check-scope.sh` before relying on results: scope changes when Microsoft ships a release.
- Never commit or publish anything under `dotnet-bounty-lab/notes/private/` or `labs/findings/` (leads, trigger inputs, probe, fuzz and audit results). Publishing before MSRC triage counts as public disclosure.
- Probes target loopback only. Do not point them at systems the owner has not said are theirs.

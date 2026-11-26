# Working rules for this repository

## Do the work yourself; subagents only when truly needed

- When given a goal, work on it directly in the main session. That is the default for everything: reading code, running commands, writing files, checking results.
- Use a subagent only when it is truly needed: independent work that would otherwise block the main thread, or a broad search whose raw output would flood the context. "It would be faster" is not enough.
- When subagents are needed, use few. Never run more than **3** concurrently; prefer 1 or 2.
- This covers every way of spawning agents: the Agent tool, forks, workflows, and remote sessions.
- If a task wants more parallelism, queue the rest and start the next agent only when one finishes.
- Workflow scripts must cap their fan-out at 3 concurrent agents, whatever the default size guideline says.

## dotnet-bounty-lab

- This repository is public. Treat every commit as published.
- Standing assignment: before any hunting work, read `dotnet-bounty-lab/notes/private/hunt/HUNT-PROMPT.md` in full, then `RESUME.md` beside it. Both are private and not in git; if they are missing, ask the owner for the assignment instead of guessing.
- Start with `dotnet-bounty-lab/README.md` and `SCOPE.md`. `scope/manifest.json` is the single source of truth for what is in scope.
- Run `dotnet-bounty-lab/scripts/00-check-scope.sh` before relying on results: scope changes when Microsoft ships a release.
- Never commit or publish anything under `dotnet-bounty-lab/notes/private/` or `labs/findings/` (leads, trigger inputs, probe, fuzz and audit results). Publishing before MSRC triage counts as public disclosure.
- Probes target loopback only. Do not point them at systems the owner has not said are theirs.

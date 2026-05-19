# Agent Instructions

## Project Context Loading

Always read:
- `docs/PROJECT_OVERVIEW.md`
- `docs/RECENT_WORK.md`

Only read:
- `docs/SESSION_NOTES/`
- `docs/ARCHIVE/`

when:
- the current task references older work
- architecture history is needed
- debugging old decisions
- implementation history matters

Also read focused source files before editing. Do not rely on memory files as a substitute for inspecting the current code.

## Context Maintenance

After meaningful changes:
- update `docs/RECENT_WORK.md`
- keep `docs/RECENT_WORK.md` short
- move older entries into `docs/SESSION_NOTES/`
- keep `docs/PROJECT_OVERVIEW.md` stable and concise
- avoid appending large logs

Use session notes for chronological implementation history. Prefer one focused note per meaningful topic/date rather than one large catch-all file.

## Memory Policy

- `PROJECT_OVERVIEW` = stable architecture memory
- `RECENT_WORK` = active short-term memory
- `SESSION_NOTES` = historical implementation memory
- `ARCHIVE` = cold storage

Future agents should not load large history files unless the task needs that history.

## Engineering Boundaries

- Keep battle rules out of `BattleSim.App`.
- Keep Avalonia references out of `BattleSim.Domain` and `BattleSim.Engine`.
- Preserve dependency direction: Domain -> none, Engine -> Domain, App -> Domain + Engine.
- Prefer incremental, testable changes over broad rewrites.
- Run `dotnet build` and `dotnet test` after code changes when practical.

## Session Note Naming

Use:

`YYYY-MM-DD-topic.md`

Examples:
- `2026-05-18-formation-builder.md`
- `2026-05-19-targeting-rules.md`
- `2026-05-19-action-resolution.md`

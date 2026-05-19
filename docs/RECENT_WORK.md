# Current Focus

Current active work:
- Keep battle action resolution separated from battle orchestration.
- Keep project context lightweight so future sessions load only current memory by default.

# Recent Additions

- Extracted action execution from `BattleEngine` into `IBattleActionResolver` / `BattleActionResolver`.
- Added structured action resolution output with `BattleActionResult` and `BattleActionTargetResult`.
- Fixed the battle log so it scrolls instead of stretching the Battle tab as entries grow.
- Added development-only persistent template saves under `DevData/UnitTemplates`.
- Fixed UnitTemplate orientation so the Formation Builder's local front row faces the opposing unit on either side.
- Added Formation Builder support for editing/saving UnitTemplates in-app.
- Cleaned up shared 3x3 grid sizing for battle and builder grids.

# Next Ideas

- Rename attack-slot terminology to action-slot terminology.
- Expand targeting rules for row/AoE actions.
- Add status effects.
- Add equipment and character customization.
- Consider JSON-loaded class/action catalogs after the C# model settles.

# Maintenance Reminder

Keep this file short: only the most recent 5-10 active items. Move older detail into `docs/SESSION_NOTES/`.

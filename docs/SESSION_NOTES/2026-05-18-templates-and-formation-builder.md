# 2026-05-18 Templates And Formation Builder

## Summary

This phase introduced saved/loadable unit templates and an in-app Formation Builder.

## UnitTemplate Layer

- Added `UnitTemplate` and `UnitTemplateTroop`.
- Templates store id, name, troop slot id, display name, troop class id, local row/column, and optional leader flag.
- Templates intentionally do not store runtime battle state such as current HP, buffs, remaining action slots, alive/dead state, or battle progress.
- `UnitTemplateValidator` enforces 1-5 troops, valid 3x3 positions, no duplicate cells, known troop class ids, unique slot ids, and at most one leader.
- Missing leader is allowed; leader-targeting rules fall back safely.
- `UnitTemplateRepository` loads and saves simple human-editable JSON.
- `UnitFactory` creates runtime `Unit`/`Troop` state from templates.

## Template Orientation

Templates are authored in local formation coordinates:

- Row 0 is the unit's front/north row in the builder.
- Column 0 is the unit's local left side.

When loaded into battle:

- Blue/left maps local front toward global right.
- Red/right maps local front toward global left.

This allows the same saved template to be loaded on either side, including mirror matches, while still facing the enemy.

## Template Storage

- Built-in sample templates live in `src/BattleSim.App/Data/UnitTemplates/`.
- The app copies built-in sample templates to output.
- Builder-created templates are development-only persistent data under `DevData/UnitTemplates` at the repo root.
- `DevData/` is ignored by git so local test templates survive clean/rebuild without being committed by default.

## Formation Builder

- Added an in-app Formation Builder tab rather than a separate project.
- The builder edits draft template data, not runtime battle units.
- It supports new draft creation, id/name editing, adding troops from known class definitions, selecting troops, removing troops, leader marking, grid placement, drag repositioning, validation, and saving.
- Saved templates refresh the same dropdown list used by the Battle tab.
- Direct Apply to Blue/Red buttons were removed; templates are loaded through the normal side dropdowns.
- Moving into an occupied builder cell is rejected for now; swapping is deferred.

## UI Layout Work

- Battle and builder grids share 108x108 cells inside a 324x324 inner grid and 326x326 frame.
- Main window default size is 1200x820 with a 1050x720 minimum.
- Battle and Formation Builder tabs use scroll viewers to prevent grid clipping on smaller windows.
- Battle grid cells removed the redundant class line under portraits to reduce clipping.
- Battle log now has its own fixed-height scrolling list so it does not stretch the Battle tab as entries grow.

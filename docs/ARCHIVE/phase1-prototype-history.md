# Phase 1 Prototype History

This archive condenses early project history that is useful context but should not be loaded by default.

## Initial Scaffold

- Created a .NET 8 solution with Domain, Engine, App, and Tests projects.
- App uses Avalonia UI and MVVM.
- Domain started with Troop, Unit, Stats, GridPosition, and TroopClass.
- Engine started with BattleState, BattleEngine, BattleStepResult, and BattleEvent.
- App started with two placeholder 3x3 grids, a battle log, and run/reset controls.

## Early Battle Progression

- Added randomized unit order.
- Added randomized but stable per-unit troop order.
- Added setup log lines showing unit order and troop order.
- Replaced a single "turn" concept with round, unit turn, and individual attack stepping.
- Added Run Next Attack, Run Next Turn, and Run One Round controls.

## Early UI Improvements

- Added a selected-log text box below the battle log.
- Added selection behavior for log entries.
- Added attack/target cell highlighting.
- Added class portraits for Wizard, Fighter, Archer, and Cleric.
- Added persistent attack arrow overlay linked to selected battle log events.
- Added pre-battle drag repositioning on the battle screen.
- Added post-start troop detail popup.

## Data Model Evolution

- Moved from `TroopClass` enum switch logic toward `TroopClassDefinition`.
- Replaced row attack counts with row action lists.
- Added built-in action definitions.
- Added stat-based scaling, accuracy, Luck, and crit data.
- Kept `TroopClass` as transitional compatibility only.

## Known Early Transitional Choices

- Many public names still say "attacks" where the newer concept is action slots.
- Built-in data remains C# catalog data.
- JSON loading exists for UnitTemplate data, but not yet for troop class or action catalogs.

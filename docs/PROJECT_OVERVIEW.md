# Project Overview

## Project

Ogre Battle style combat simulator: a systems-focused tactical battle prototype and visualization tool, not a full RPG.

The app is a .NET 8 C# desktop application using Avalonia UI and MVVM. The primary design rule is to keep battle rules out of the UI so the core model remains portable, including a possible later Unity port.

## Architecture

- `BattleSim.Domain`: pure models, enums, class/action definitions, targeting rule interfaces, unit template validation and JSON repository code. No Avalonia references.
- `BattleSim.Engine`: battle setup, runtime unit creation, battle progression, formation-facing rules, action resolution orchestration, and combat execution. Depends on Domain only.
- `BattleSim.App`: Avalonia views and view models for the battle simulator and Formation Builder. Depends on Domain and Engine.
- `BattleSim.Tests`: xUnit tests for domain, engine, targeting, templates, and selected app view-model behavior.

Dependency direction should remain one-way: Domain -> none, Engine -> Domain, App -> Domain + Engine.

## Core Concepts

- `TroopClassDefinition`: reusable class/rules data. Built-ins include Fighter, Archer, Cleric, and Wizard. A definition owns base stats, row action profile, display name, id, and a prototype portrait asset path.
- `BattleActionDefinition`: data for an action such as Slash, Bow Shot, Staff Bonk, Firebolt, or Heal. It includes action kind, target side, targeting rule object, base power, stat scaling, accuracy, and crit eligibility.
- `RowActionProfile`: ordered action lists by formation rank: Front, Middle, Back.
- `UnitTemplate`: saved composition/default formation data. It stores troop class ids and local grid positions, not runtime HP or battle state.
- Runtime `Unit` / `Troop`: battle-time state created from templates or defaults. Troops own current HP, position, and remaining action slots.
- `UnitFactory`: maps local UnitTemplate positions into side-facing runtime positions. A template's row 0 is its local front; on the battle screen Blue faces right and Red faces left.
- `BattleEngine`: orchestrates battle lifecycle, actor/action selection, target selection, and progression.
- `BattleActionResolver`: resolves one chosen action against selected targets, including hit/miss, Luck retry, crit, damage, healing, HP mutation, and action result events.
- `ITargetingRule`: reusable targeting rule interface. Current rules include melee, ranged, most damaged ally, and an unused entire-unit rule for future AoE actions.
- Formation Builder: in-app template authoring tab that edits draft UnitTemplate data and saves JSON through the same validation/repository path used by battle loading.

## Current Implemented Systems

- Two-side 3x3 battle visualization with Blue and Red units facing each other.
- Deterministic step controls: Run Next Attack, Run Next Turn, Run One Round, Reset Battle.
- Randomized unit order and per-unit troop order chosen at battle setup.
- Per-row action slots represented as repeated actions in `RowActionProfile`.
- Battle completion when all living troops exhaust their action slots.
- Formation rotation before battle starts.
- Pre-battle troop repositioning by drag within a unit.
- Post-start troop detail popup with stats, position, next action, and current row actions.
- Data-driven troop classes and actions in C# catalogs.
- Accuracy, Luck-based miss retry, crit chance, crit bonus damage, stat scaling, healing, and defense subtraction.
- Interface-based targeting rules with Normal, Weakest, and Leader unit targeting preferences.
- Unit leaders with current visual marking only.
- Saved/loadable UnitTemplate JSON files.
- Development-only persistent saved templates under `DevData/UnitTemplates`.
- Formation Builder tab for creating/editing/saving templates.
- Prototype class portrait PNGs rendered in battle and builder grids.
- Battle log selection, selected-log detail text box, attacker/target highlights, and persistent action arrow overlay.

## Data Locations

- Built-in sample templates: `src/BattleSim.App/Data/UnitTemplates/`
- Development-only saved templates: `DevData/UnitTemplates/` at repo root, ignored by git.
- Portrait assets: `Assets/Images/Portraits/`
- Targeting rules reference: `TARGETING_RULES.txt`

## Current Built-In Class Patterns

- Fighter: Slash x3 front, Slash x2 middle, Slash x1 back.
- Archer: Bow Shot x1 front, Bow Shot x2 middle, Bow Shot x3 back.
- Cleric: Staff Bonk x1 front, Heal x2 middle, Heal x2 back.
- Wizard: Staff Bonk x1 front, Firebolt x3 middle, Firebolt x2 back.

Current base HP pools are Fighter 48, Archer 40, Cleric 36, Wizard 32.

## Planned Systems

- Rename remaining attack-slot terminology to action-slot terminology.
- Expand targeting with row attacks, AoE, and more specific melee/ranged rules.
- Add status effects.
- Add equipment and character customization.
- Add AI/order behavior beyond the current Normal/Weakest/Leader preferences.
- Move hardcoded class/action catalogs toward loadable data once the model stabilizes.
- Keep models portable enough for a later Unity adaptation.

## Transitional Notes

- `TroopClass` enum and a legacy `Troop` constructor still exist for compatibility.
- Some UI and engine property names still say "attacks" even though the concept is now action slots.
- Built-in class/action data is still hardcoded C#.
- The app has prototype-oriented UI code for overlays and drag behavior; combat rules should continue to stay in Domain/Engine.

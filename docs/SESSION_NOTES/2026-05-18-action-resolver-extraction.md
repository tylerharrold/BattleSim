# 2026-05-18 Action Resolver Extraction

## Summary

Action execution was extracted from `BattleEngine` so the engine can stay focused on orchestration.

## What Moved Out Of BattleEngine

- Spending the action slot.
- No-target handling.
- Accuracy hit/miss roll.
- Luck retry roll after a miss.
- Critical roll and critical bonus damage.
- Damage amount rolling and Defense subtraction.
- Healing amount rolling and HP restoration.
- HP mutation.
- Per-target structured result creation.
- Per-target `BattleEvent` creation.

## New Engine Types

- `IBattleActionResolver`
- `BattleActionResolver`
- `BattleActionResolutionRequest`
- `BattleActionResult`
- `BattleActionTargetResult`
- `BattleActionOutcome`

## Remaining BattleEngine Responsibilities

- Clone/progress battle state.
- Start rounds and unit turns.
- Choose the next scheduled troop.
- Choose that troop's next row action.
- Build `TargetingContext`.
- Ask the action's `ITargetingRule` for targets.
- Call `BattleActionResolver`.
- Advance troop/unit/round progression.

## Why This Matters

This creates a clearer extension point for future action behavior such as buffs, debuffs, summons, status effects, equipment modifiers, and multi-target actions without continuing to enlarge `BattleEngine`.

## Validation

Resolver-focused tests cover damage, miss, crit, healing, and no-target behavior. Existing battle flow tests still cover progression.

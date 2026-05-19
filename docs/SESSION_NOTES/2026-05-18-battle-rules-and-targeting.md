# 2026-05-18 Battle Rules And Targeting

## Summary

This phase moved the simulator from basic "fight until one side is dead" combat toward an Ogre Battle style action-slot flow with data-driven actions and targeting rules.

## Battle Flow

- Unit order is chosen randomly at battle setup.
- Troop order inside each unit is chosen randomly once at battle setup and remains stable.
- Each troop acts according to its row-based action list.
- Battle completion now occurs when all living troops have exhausted their action slots, not when one side is wiped out.
- The UI supports stepping by one attack, one unit turn, or one full round.

## Data-Driven Class And Action Work

- Troop classes moved from switch-based attack counts to `TroopClassDefinition`.
- `RowAttackProfile` was replaced with `RowActionProfile`.
- Class rows now contain ordered `BattleActionDefinition` lists.
- Built-in actions include Slash, Bow Shot, Staff Bonk, Firebolt, and Heal.
- `BattleActionDefinition` gained action kind, target side, targeting rule object, base power, stat scaling, accuracy, and crit eligibility.

## Stats And Combat Math

- `Stats` expanded to MaxHitPoints, Strength, Defense, Speed, Faith, Wisdom, Dexterity, and Luck.
- The old Attack stat became Strength.
- Actions scale from a configured stat using a min/max percent-per-stat-point range.
- Damage subtracts Defense after scaling.
- Accuracy controls the initial hit roll.
- Luck can grant one post-miss retry and separately affects crit chance.
- Critical hits add a second scaled damage amount multiplied by the current crit scalar.
- Heal uses Faith scaling and currently cannot crit.

## Targeting

- Targeting moved from string rule ids to `ITargetingRule` objects.
- `TargetSelection` can return multiple targets, though current built-ins mostly return one.
- Added melee, ranged, most-damaged-ally, and entire-unit targeting rules.
- Unit targeting preferences were added: Normal, Weakest, Leader.
- Ranged/spell attacks can target any enemy and use highest proportional HP as normal preference.
- Melee uses lane/blocker logic and can target a blocker when carving a path to a preferred leader/weakest target.
- Heal targets the most damaged ally, falling back to the lowest Max HP living ally when no one is injured.
- Entire-unit targeting is stubbed for future AoE actions.

## UI Support Added During This Phase

- Battle log selected-line text box.
- Attack log selection highlights actor and target cells.
- Helpful events use blue target highlights and green arrows.
- Harmful events use yellow target highlights and red arrows.
- Self-heal hides the arrow and highlights only the target cell.
- Persistent action arrow overlay can reappear when selecting older log entries.

## Reference

Detailed current targeting behavior is documented in `TARGETING_RULES.txt`.

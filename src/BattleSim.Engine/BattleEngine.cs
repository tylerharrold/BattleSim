using BattleSim.Domain.Models;
using BattleSim.Domain.Enums;
using BattleSim.Domain.Targeting;

namespace BattleSim.Engine;

public sealed class BattleEngine
{
    private readonly Random random;

    public BattleEngine()
        : this(Random.Shared)
    {
    }

    public BattleEngine(Random random)
    {
        this.random = random;
    }

    public BattleStepResult RunNextAttack(BattleState state)
    {
        // The engine is the only place that mutates battle progress; UI code receives a result to render.
        if (state.IsComplete)
        {
            return new BattleStepResult(state, Array.Empty<BattleEvent>());
        }

        var nextState = state.CloneForProgress();
        var events = new List<BattleEvent>();

        while (!nextState.IsComplete)
        {
            AddPhaseStartEvents(nextState, events);

            var nextAttacker = GetNextScheduledAttacker(nextState);

            if (nextAttacker is not null)
            {
                var defenderSide = nextState.CurrentSide == BattleSide.Left ? BattleSide.Right : BattleSide.Left;
                var action = GetNextBattleAction(nextAttacker.Troop, nextState.CurrentSide);
                ResolveAction(
                    nextAttacker.Troop,
                    action,
                    nextState.CurrentSide,
                    nextState.GetUnit(nextState.CurrentSide),
                    nextState.GetOpponent(nextState.CurrentSide),
                    defenderSide,
                    events);
                nextState = AdvanceAfterAttack(nextState, nextAttacker.TroopOrderIndex);
                break;
            }

            nextState = AdvanceToNextUnitOrRound(nextState);
        }

        return new BattleStepResult(nextState, events);
    }

    public BattleStepResult RunNextTurn(BattleState state)
    {
        if (state.IsComplete)
        {
            return new BattleStepResult(state, Array.Empty<BattleEvent>());
        }

        var turnSide = state.CurrentSide;
        var result = RunNextAttack(state);
        var events = result.Events.ToList();
        var nextState = result.State;
        turnSide = events.FirstOrDefault(battleEvent => battleEvent.ActorSide.HasValue)?.ActorSide ?? turnSide;

        while (!nextState.IsComplete && nextState.HasRoundStarted && nextState.CurrentSide == turnSide)
        {
            result = RunNextAttack(nextState);
            nextState = result.State;
            events.AddRange(result.Events);
        }

        return new BattleStepResult(nextState, events);
    }

    public BattleStepResult RunOneRound(BattleState state)
    {
        if (state.IsComplete)
        {
            return new BattleStepResult(state, Array.Empty<BattleEvent>());
        }

        var startingRound = state.RoundNumber;
        var events = new List<BattleEvent>();
        var nextState = state;

        while (!nextState.IsComplete && nextState.RoundNumber == startingRound)
        {
            var result = RunNextAttack(nextState);
            nextState = result.State;
            events.AddRange(result.Events);
        }

        return new BattleStepResult(nextState, events);
    }

    private static void AddPhaseStartEvents(BattleState state, ICollection<BattleEvent> events)
    {
        if (!state.HasRoundStarted)
        {
            events.Add(new BattleEvent($"Round {state.RoundNumber} begins."));
        }

        if (state.TroopOrderIndex == 0)
        {
            events.Add(new BattleEvent($"{state.GetUnit(state.CurrentSide).Name} attacks."));
        }
    }

    private static ScheduledAttacker? GetNextScheduledAttacker(BattleState state)
    {
        var attackerUnit = state.GetUnit(state.CurrentSide);
        var troopOrder = state.Plan.TroopOrders[state.CurrentSide];

        for (var index = state.TroopOrderIndex; index < troopOrder.Count; index++)
        {
            var troopName = troopOrder[index];
            var attacker = attackerUnit.Troops.FirstOrDefault(troop => troop.Name == troopName);

            if (attacker is not null && attacker.CanAttack)
            {
                return new ScheduledAttacker(attacker, index);
            }
        }

        return null;
    }

    private static BattleActionDefinition GetNextBattleAction(Troop troop, BattleSide side)
    {
        var actions = BattleActionRules.GetBattleActions(troop, side);
        var actionIndex = troop.MaxBattleAttacks - troop.RemainingBattleAttacks;

        return actions[actionIndex];
    }

    private void ResolveAction(
        Troop attacker,
        BattleActionDefinition action,
        BattleSide attackerSide,
        Unit attackerUnit,
        Unit defenderUnit,
        BattleSide defenderSide,
        ICollection<BattleEvent> events)
    {
        var targetingContext = new TargetingContext(attacker, attackerUnit, defenderUnit);
        var targets = action.TargetingRule.SelectTargets(targetingContext, action);

        if (!targets.HasTargets)
        {
            attacker.SpendBattleAttack();
            events.Add(new BattleEvent(
                $"{attacker.Name} uses {action.DisplayName}, but has no valid target. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
                attacker.Name,
                Damage: 0,
                ActorSide: attackerSide,
                ActorPosition: attacker.Position,
                Intent: GetEventIntent(action)));
            return;
        }

        attacker.SpendBattleAttack();

        foreach (var target in targets.Targets)
        {
            events.Add(ResolveActionAgainstTarget(
                attacker,
                action,
                target,
                attackerSide,
                GetTargetSide(action, attackerSide, defenderSide)));
        }
    }

    private BattleEvent ResolveActionAgainstTarget(
        Troop attacker,
        BattleActionDefinition action,
        Troop target,
        BattleSide attackerSide,
        BattleSide targetSide)
    {
        if (action.ActionKind == ActionKind.Heal)
        {
            var healing = CalculateActionAmount(attacker, action, target, appliesDefense: false);
            var missingHitPoints = target.Stats.MaxHitPoints - target.CurrentHitPoints;
            var actualHealing = Math.Min(healing, missingHitPoints);
            target.Heal(healing);

            return new BattleEvent(
                $"{attacker.Name} uses {action.DisplayName} on {target.Name} for {actualHealing} healing. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
                attacker.Name,
                target.Name,
                Damage: 0,
                ActorSide: attackerSide,
                ActorPosition: attacker.Position,
                TargetSide: targetSide,
                TargetPosition: target.Position,
                Intent: GetEventIntent(action));
        }

        var damage = CalculateActionAmount(attacker, action, target, appliesDefense: true);
        target.TakeDamage(damage);

        return new BattleEvent(
            $"{attacker.Name} uses {action.DisplayName} on {target.Name} for {damage} damage. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
            attacker.Name,
            target.Name,
            damage,
            attackerSide,
            attacker.Position,
            targetSide,
            target.Position,
            GetEventIntent(action));
    }

    private int CalculateActionAmount(Troop attacker, BattleActionDefinition action, Troop target, bool appliesDefense)
    {
        // Accuracy is stored on the action for the next rules pass; this prototype still assumes actions land.
        var scalingStat = GetScalingStat(attacker.Stats, action.Scaling.Stat);
        var multiplier = action.Scaling.RollMultiplier(scalingStat, random);
        var scaledAmount = Math.Max(1, (int)Math.Round(action.BasePower * multiplier, MidpointRounding.AwayFromZero));

        return appliesDefense
            ? Math.Max(1, scaledAmount - target.Stats.Defense)
            : scaledAmount;
    }

    private static int GetScalingStat(Stats stats, CombatStat stat)
    {
        return stat switch
        {
            CombatStat.Strength => stats.Strength,
            CombatStat.Faith => stats.Faith,
            CombatStat.Wisdom => stats.Wisdom,
            CombatStat.Dexterity => stats.Dexterity,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
    }

    private static BattleSide GetTargetSide(BattleActionDefinition action, BattleSide attackerSide, BattleSide defenderSide)
    {
        return action.TargetSide == TargetSide.Ally ? attackerSide : defenderSide;
    }

    private static BattleEventIntent GetEventIntent(BattleActionDefinition action)
    {
        return action.ActionKind == ActionKind.Heal
            ? BattleEventIntent.Helpful
            : BattleEventIntent.Harmful;
    }

    private static BattleState AdvanceAfterAttack(BattleState state, int completedTroopOrderIndex)
    {
        var nextTroopIndex = completedTroopOrderIndex + 1;

        return nextTroopIndex >= state.Plan.TroopOrders[state.CurrentSide].Count
            ? AdvanceToNextUnitOrRound(state)
            : state.WithProgress(state.RoundNumber, state.UnitOrderIndex, nextTroopIndex, hasRoundStarted: true);
    }

    private static BattleState AdvanceToNextUnitOrRound(BattleState state)
    {
        var nextUnitIndex = state.UnitOrderIndex + 1;

        if (nextUnitIndex < state.Plan.UnitOrder.Count)
        {
            return state.WithProgress(state.RoundNumber, nextUnitIndex, troopOrderIndex: 0, hasRoundStarted: true);
        }

        return state.WithProgress(
            state.RoundNumber + 1,
            unitOrderIndex: 0,
            troopOrderIndex: 0,
            hasRoundStarted: false);
    }

    private sealed record ScheduledAttacker(Troop Troop, int TroopOrderIndex);
}

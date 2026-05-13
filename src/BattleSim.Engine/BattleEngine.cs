using BattleSim.Domain.Models;
using BattleSim.Domain.Enums;

namespace BattleSim.Engine;

public sealed class BattleEngine
{
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

    private static void ResolveAction(
        Troop attacker,
        BattleActionDefinition action,
        BattleSide attackerSide,
        Unit defenderUnit,
        BattleSide defenderSide,
        ICollection<BattleEvent> events)
    {
        if (action.ActionKind == ActionKind.Heal)
        {
            // TODO: Resolve ally targeting from action.TargetSide and action.TargetingRuleId.
            attacker.SpendBattleAttack();
            events.Add(new BattleEvent(
                $"{attacker.Name} uses {action.DisplayName}, but healing is not implemented yet. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
                attacker.Name,
                Damage: 0,
                ActorSide: attackerSide,
                ActorPosition: attacker.Position));
            return;
        }

        // TODO: Resolve targets from action.TargetSide and action.TargetingRuleId.
        // Targeting stays deterministic for now so this step only changes action data, not target selection.
        var defender = defenderUnit.LivingTroops
            .OrderBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();

        if (defender is null)
        {
            attacker.SpendBattleAttack();
            events.Add(new BattleEvent(
                $"{attacker.Name} uses {action.DisplayName}, but has no living target. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
                attacker.Name,
                Damage: 0,
                ActorSide: attackerSide,
                ActorPosition: attacker.Position));
            return;
        }

        var baseDamage = Math.Max(1, attacker.Stats.Attack - defender.Stats.Defense);
        var damage = Math.Max(1, (int)Math.Round(baseDamage * action.Power, MidpointRounding.AwayFromZero));
        defender.TakeDamage(damage);
        attacker.SpendBattleAttack();

        events.Add(new BattleEvent(
            $"{attacker.Name} uses {action.DisplayName} on {defender.Name} for {damage} damage. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
            attacker.Name,
            defender.Name,
            damage,
            attackerSide,
            attacker.Position,
            defenderSide,
            defender.Position));
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

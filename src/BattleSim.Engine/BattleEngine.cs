using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed class BattleEngine
{
    public BattleStepResult RunOneRound(BattleState state)
    {
        // The engine is the only place that mutates battle progress; UI code receives a result to render.
        if (state.IsComplete)
        {
            return new BattleStepResult(state, Array.Empty<BattleEvent>());
        }

        var nextState = state.AdvanceRound();
        var events = new List<BattleEvent>();

        events.Add(new BattleEvent($"Round {state.RoundNumber} begins."));

        foreach (var side in nextState.Plan.UnitOrder)
        {
            ResolveUnitRound(nextState, side, events);

            if (nextState.IsComplete)
            {
                break;
            }
        }

        return new BattleStepResult(nextState, events);
    }

    private static void ResolveUnitRound(BattleState state, BattleSide attackerSide, ICollection<BattleEvent> events)
    {
        var attackerUnit = state.GetUnit(attackerSide);
        var defenderUnit = state.GetOpponent(attackerSide);

        events.Add(new BattleEvent($"{attackerUnit.Name} attacks."));

        foreach (var troopName in state.Plan.TroopOrders[attackerSide])
        {
            var attacker = attackerUnit.Troops.FirstOrDefault(troop => troop.Name == troopName);

            if (attacker is null || attacker.IsDefeated)
            {
                continue;
            }

            ResolveAttack(attacker, defenderUnit, events);

            if (defenderUnit.IsDefeated)
            {
                break;
            }
        }
    }

    private static void ResolveAttack(Troop attacker, Unit defenderUnit, ICollection<BattleEvent> events)
    {
        // Targeting stays deterministic for now so the only randomness is battle setup order.
        var defender = defenderUnit.LivingTroops
            .OrderBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();

        if (defender is null)
        {
            return;
        }

        var damage = Math.Max(1, attacker.Stats.Attack - defender.Stats.Defense);
        defender.TakeDamage(damage);

        events.Add(new BattleEvent(
            $"{attacker.Name} hits {defender.Name} for {damage} damage.",
            attacker.Name,
            defender.Name,
            damage,
            attacker.Position,
            defender.Position));
    }
}

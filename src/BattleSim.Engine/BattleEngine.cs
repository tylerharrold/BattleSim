using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed class BattleEngine
{
    public BattleStepResult RunOneTurn(BattleState state)
    {
        // The engine is the only place that mutates battle progress; UI code receives a result to render.
        if (state.IsComplete)
        {
            return new BattleStepResult(state, Array.Empty<BattleEvent>());
        }

        var nextState = state.AdvanceTurn();
        var events = new List<BattleEvent>();

        ResolveAttack(nextState.LeftUnit, nextState.RightUnit, events);

        if (!nextState.RightUnit.IsDefeated)
        {
            ResolveAttack(nextState.RightUnit, nextState.LeftUnit, events);
        }

        return new BattleStepResult(nextState, events);
    }

    private static void ResolveAttack(Unit attackerUnit, Unit defenderUnit, ICollection<BattleEvent> events)
    {
        // Deterministic placeholder rule: fastest living troop attacks the first living enemy in reading order.
        var attacker = attackerUnit.LivingTroops
            .OrderByDescending(troop => troop.Stats.Speed)
            .ThenBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();

        var defender = defenderUnit.LivingTroops
            .OrderBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();

        if (attacker is null || defender is null)
        {
            return;
        }

        var damage = Math.Max(1, attacker.Stats.Attack - defender.Stats.Defense);
        defender.TakeDamage(damage);

        events.Add(new BattleEvent(
            attacker.Name,
            defender.Name,
            damage,
            $"{attacker.Name} hits {defender.Name} for {damage} damage.",
            attacker.Position,
            defender.Position));
    }
}

using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed class BattleActionResolver : IBattleActionResolver
{
    private readonly Random random;

    public BattleActionResolver()
        : this(Random.Shared)
    {
    }

    public BattleActionResolver(Random random)
    {
        this.random = random;
    }

    public BattleActionResult Resolve(BattleActionResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Targets.Count == 0)
        {
            request.Attacker.SpendBattleAttack();
            var noTargetEvent = new BattleEvent(
                $"{request.Attacker.Name} uses {request.Action.DisplayName}, but has no valid target. {request.Attacker.Name} has {request.Attacker.RemainingBattleAttacks} attacks left.",
                request.Attacker.Name,
                Damage: 0,
                ActorSide: request.AttackerSide,
                ActorPosition: request.Attacker.Position,
                Intent: GetEventIntent(request.Action));

            return new BattleActionResult(
                BattleActionOutcome.NoValidTargets,
                request.Attacker,
                request.Action,
                Array.Empty<BattleActionTargetResult>(),
                new[] { noTargetEvent });
        }

        request.Attacker.SpendBattleAttack();

        var targetResults = new List<BattleActionTargetResult>();
        var events = new List<BattleEvent>();

        foreach (var target in request.Targets)
        {
            var targetResult = ResolveAgainstTarget(
                request.Attacker,
                request.Action,
                target,
                request.AttackerSide,
                request.TargetSide);

            targetResults.Add(targetResult);
            events.Add(targetResult.Event);
        }

        return new BattleActionResult(
            BattleActionOutcome.Completed,
            request.Attacker,
            request.Action,
            targetResults,
            events);
    }

    private BattleActionTargetResult ResolveAgainstTarget(
        Troop attacker,
        BattleActionDefinition action,
        Troop target,
        BattleSide attackerSide,
        BattleSide targetSide)
    {
        if (action.ActionKind == ActionKind.Heal)
        {
            return ResolveHealing(attacker, action, target, attackerSide, targetSide);
        }

        return ResolveDamage(attacker, action, target, attackerSide, targetSide);
    }

    private BattleActionTargetResult ResolveHealing(
        Troop attacker,
        BattleActionDefinition action,
        Troop target,
        BattleSide attackerSide,
        BattleSide targetSide)
    {
        var healing = RollActionAmount(attacker, action, target, appliesDefense: false);
        var missingHitPoints = target.Stats.MaxHitPoints - target.CurrentHitPoints;
        var actualHealing = Math.Min(healing, missingHitPoints);
        target.Heal(healing);

        var battleEvent = new BattleEvent(
            $"{attacker.Name} uses {action.DisplayName} on {target.Name} for {actualHealing} healing. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
            attacker.Name,
            target.Name,
            Damage: 0,
            ActorSide: attackerSide,
            ActorPosition: attacker.Position,
            TargetSide: targetSide,
            TargetPosition: target.Position,
            Intent: GetEventIntent(action));

        return new BattleActionTargetResult(
            target,
            DidHit: true,
            WasLuckReroll: false,
            WasCritical: false,
            DamageDealt: 0,
            HealingDone: actualHealing,
            WasDefeated: target.IsDefeated,
            battleEvent);
    }

    private BattleActionTargetResult ResolveDamage(
        Troop attacker,
        BattleActionDefinition action,
        Troop target,
        BattleSide attackerSide,
        BattleSide targetSide)
    {
        var rollResult = ResolveDamageRoll(attacker, action, target);

        if (!rollResult.DidHit)
        {
            var missEvent = new BattleEvent(
                $"{attacker.Name} uses {action.DisplayName} on {target.Name}, but misses. {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.",
                attacker.Name,
                target.Name,
                Damage: 0,
                ActorSide: attackerSide,
                ActorPosition: attacker.Position,
                TargetSide: targetSide,
                TargetPosition: target.Position,
                Intent: GetEventIntent(action));

            return new BattleActionTargetResult(
                target,
                DidHit: false,
                rollResult.WasLuckReroll,
                WasCritical: false,
                DamageDealt: 0,
                HealingDone: 0,
                WasDefeated: target.IsDefeated,
                missEvent);
        }

        target.TakeDamage(rollResult.TotalDamage);

        var damageEvent = new BattleEvent(
            FormatDamageEvent(attacker, action, target, rollResult),
            attacker.Name,
            target.Name,
            rollResult.TotalDamage,
            attackerSide,
            attacker.Position,
            targetSide,
            target.Position,
            GetEventIntent(action));

        return new BattleActionTargetResult(
            target,
            DidHit: true,
            rollResult.WasLuckReroll,
            rollResult.WasCritical,
            DamageDealt: rollResult.TotalDamage,
            HealingDone: 0,
            WasDefeated: target.IsDefeated,
            damageEvent);
    }

    private CombatRollResult ResolveDamageRoll(Troop attacker, BattleActionDefinition action, Troop target)
    {
        var didHit = RollChance(action.Accuracy);
        var wasLuckReroll = false;

        if (!didHit && RollChance(CombatRollRules.GetLuckRerollChance(attacker.Stats.Luck)))
        {
            wasLuckReroll = true;
            didHit = RollChance(action.Accuracy);
        }

        if (!didHit)
        {
            return new CombatRollResult(
                DidHit: false,
                WasLuckReroll: wasLuckReroll,
                WasCritical: false,
                BaseDamage: 0,
                CriticalBonusDamage: 0);
        }

        var baseDamage = RollActionAmount(attacker, action, target, appliesDefense: true);
        var wasCritical = action.CanCrit && RollChance(CombatRollRules.GetCriticalChance(attacker.Stats.Luck));
        var criticalBonusDamage = wasCritical
            ? Math.Max(1, (int)Math.Round(RollActionAmount(attacker, action, target, appliesDefense: true) * CombatRollRules.CriticalDamageScalar, MidpointRounding.AwayFromZero))
            : 0;

        return new CombatRollResult(
            DidHit: true,
            WasLuckReroll: wasLuckReroll,
            WasCritical: wasCritical,
            BaseDamage: baseDamage,
            CriticalBonusDamage: criticalBonusDamage);
    }

    private int RollActionAmount(Troop attacker, BattleActionDefinition action, Troop target, bool appliesDefense)
    {
        var scalingStat = CombatRollRules.GetScalingStat(attacker.Stats, action.Scaling.Stat);
        var multiplier = action.Scaling.RollMultiplier(scalingStat, random);
        var scaledAmount = Math.Max(1, (int)Math.Round(action.BasePower * multiplier, MidpointRounding.AwayFromZero));

        return appliesDefense
            ? Math.Max(1, scaledAmount - target.Stats.Defense)
            : scaledAmount;
    }

    private bool RollChance(decimal chance)
    {
        return (decimal)random.NextDouble() < Math.Clamp(chance, 0m, 1m);
    }

    private static string FormatDamageEvent(Troop attacker, BattleActionDefinition action, Troop target, CombatRollResult result)
    {
        var luckRerollText = result.WasLuckReroll ? " after a lucky retry" : string.Empty;
        var critText = result.WasCritical ? $" Critical hit adds {result.CriticalBonusDamage} damage." : string.Empty;

        return $"{attacker.Name} uses {action.DisplayName} on {target.Name}{luckRerollText} for {result.TotalDamage} damage.{critText} {attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.";
    }

    private static BattleEventIntent GetEventIntent(BattleActionDefinition action)
    {
        return action.ActionKind == ActionKind.Heal
            ? BattleEventIntent.Helpful
            : BattleEventIntent.Harmful;
    }
}

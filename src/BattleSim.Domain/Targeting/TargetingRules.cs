using BattleSim.Domain.Models;
using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Targeting;

public sealed class MeleeTargetingRule : ITargetingRule
{
    public TargetSelection SelectTargets(TargetingContext context, BattleActionDefinition action)
    {
        return TargetSelection.Single(
            TargetingRuleHelpers.GetOnlyLivingEnemy(context)
            ?? TargetingRuleHelpers.GetPreferredEnemy(context, canTargetAnyEnemy: false)
            ?? TargetingRuleHelpers.GetDefaultMeleeTarget(context));
    }
}

public sealed class RangedTargetingRule : ITargetingRule
{
    public TargetSelection SelectTargets(TargetingContext context, BattleActionDefinition action)
    {
        return TargetSelection.Single(
            TargetingRuleHelpers.GetPreferredEnemy(context, canTargetAnyEnemy: true)
            ?? TargetingRuleHelpers.GetDefaultRangedTarget(context));
    }
}

public sealed class MostDamagedAllyTargetingRule : ITargetingRule
{
    public TargetSelection SelectTargets(TargetingContext context, BattleActionDefinition action)
    {
        var damagedTarget = context.Allies.LivingTroops
            .Where(troop => troop.CurrentHitPoints < troop.Stats.MaxHitPoints)
            .OrderByDescending(troop => troop.Stats.MaxHitPoints - troop.CurrentHitPoints)
            .ThenBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();

        var target = damagedTarget ?? context.Allies.LivingTroops
            .OrderBy(troop => troop.Stats.MaxHitPoints)
            .ThenBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();

        return TargetSelection.Single(target);
    }
}

public sealed class EntireUnitTargetingRule : ITargetingRule
{
    public TargetSelection SelectTargets(TargetingContext context, BattleActionDefinition action)
    {
        // Area actions are inherently broad and intentionally ignore Normal/Weakest/Leader preferences.
        var targetUnit = action.TargetSide == TargetSide.Ally
            ? context.Allies
            : context.Enemies;

        return TargetSelection.From(targetUnit.LivingTroops);
    }
}

internal static class TargetingRuleHelpers
{
    public static Troop? GetPreferredEnemy(TargetingContext context, bool canTargetAnyEnemy)
    {
        var preferredTarget = context.Allies.TargetingPreference switch
        {
            TargetingPreference.Leader => GetLivingLeader(context.Enemies),
            TargetingPreference.Weakest => GetWeakestLivingEnemy(context),
            _ => null
        };

        if (preferredTarget is null)
        {
            return null;
        }

        if (canTargetAnyEnemy || IsMeleeReachable(context, preferredTarget))
        {
            return preferredTarget;
        }

        return GetBlockingMeleeTarget(context, preferredTarget);
    }

    public static Troop? GetOnlyLivingEnemy(TargetingContext context)
    {
        var livingEnemies = context.Enemies.LivingTroops.Take(2).ToArray();

        return livingEnemies.Length == 1 ? livingEnemies[0] : null;
    }

    public static Troop? GetDefaultRangedTarget(TargetingContext context)
    {
        return context.Enemies.LivingTroops
            .OrderByDescending(GetProportionalHealth)
            .ThenBy(troop => GetBattleDistance(context, troop))
            .ThenBy(_ => context.Random.Next())
            .FirstOrDefault();
    }

    public static Troop? GetDefaultMeleeTarget(TargetingContext context)
    {
        var directTarget = GetFrontMostLivingEnemyInLane(context, context.Attacker.Position.Row);
        if (directTarget is not null)
        {
            return directTarget;
        }

        var adjacentTargets = GetAdjacentRows(context.Attacker.Position.Row)
            .Select(row => GetFrontMostLivingEnemyInLane(context, row))
            .Where(target => target is not null)
            .Cast<Troop>()
            .ToArray();

        return adjacentTargets
            .OrderByDescending(GetProportionalHealth)
            .ThenBy(target => GetBattleDistance(context, target))
            .ThenBy(_ => context.Random.Next())
            .FirstOrDefault();
    }

    private static Troop? GetLivingLeader(Unit unit)
    {
        return unit.LivingTroops.FirstOrDefault(troop => troop.Name == unit.LeaderName);
    }

    private static Troop? GetWeakestLivingEnemy(TargetingContext context)
    {
        return context.Enemies.LivingTroops
            .OrderBy(troop => troop.CurrentHitPoints)
            .ThenByDescending(GetProportionalHealth)
            .ThenBy(troop => GetBattleDistance(context, troop))
            .ThenBy(_ => context.Random.Next())
            .FirstOrDefault();
    }

    private static bool IsMeleeReachable(TargetingContext context, Troop target)
    {
        return Math.Abs(target.Position.Row - context.Attacker.Position.Row) <= 1 &&
            GetFrontMostLivingEnemyInLane(context, target.Position.Row) == target;
    }

    private static Troop? GetBlockingMeleeTarget(TargetingContext context, Troop preferredTarget)
    {
        if (Math.Abs(preferredTarget.Position.Row - context.Attacker.Position.Row) > 1)
        {
            return null;
        }

        var frontMostTarget = GetFrontMostLivingEnemyInLane(context, preferredTarget.Position.Row);

        return frontMostTarget is not null && frontMostTarget != preferredTarget
            ? frontMostTarget
            : null;
    }

    private static Troop? GetFrontMostLivingEnemyInLane(TargetingContext context, int row)
    {
        return context.Enemies.LivingTroops
            .Where(troop => troop.Position.Row == row)
            .OrderBy(troop => GetDepthOrder(troop.Position.Column, context.EnemiesOrientation))
            .FirstOrDefault();
    }

    private static int GetDepthOrder(int column, FormationOrientation orientation)
    {
        return orientation == FormationOrientation.FrontOnLeft ? column : 2 - column;
    }

    private static int GetBattleDistance(TargetingContext context, Troop target)
    {
        return Math.Abs(GetBattleColumn(context.Attacker.Position, context.AlliesOrientation) - GetBattleColumn(target.Position, context.EnemiesOrientation)) +
            Math.Abs(context.Attacker.Position.Row - target.Position.Row);
    }

    private static int GetBattleColumn(GridPosition position, FormationOrientation orientation)
    {
        return orientation == FormationOrientation.FrontOnRight
            ? position.Column
            : 3 + position.Column;
    }

    private static IEnumerable<int> GetAdjacentRows(int row)
    {
        if (row - 1 >= 0)
        {
            yield return row - 1;
        }

        if (row + 1 <= 2)
        {
            yield return row + 1;
        }
    }

    private static decimal GetProportionalHealth(Troop troop)
    {
        return troop.Stats.MaxHitPoints == 0
            ? 0m
            : (decimal)troop.CurrentHitPoints / troop.Stats.MaxHitPoints;
    }
}

using BattleSim.Domain.Models;

namespace BattleSim.Domain.Targeting;

public sealed class MeleeTargetingRule : ITargetingRule
{
    public TargetSelection SelectTargets(TargetingContext context, BattleActionDefinition action)
    {
        return TargetSelection.Single(FirstLivingEnemy(context));
    }

    private static Troop? FirstLivingEnemy(TargetingContext context)
    {
        return context.Enemies.LivingTroops
            .OrderBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();
    }
}

public sealed class RangedTargetingRule : ITargetingRule
{
    public TargetSelection SelectTargets(TargetingContext context, BattleActionDefinition action)
    {
        return TargetSelection.Single(FirstLivingEnemy(context));
    }

    private static Troop? FirstLivingEnemy(TargetingContext context)
    {
        return context.Enemies.LivingTroops
            .OrderBy(troop => troop.Position.Row)
            .ThenBy(troop => troop.Position.Column)
            .FirstOrDefault();
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

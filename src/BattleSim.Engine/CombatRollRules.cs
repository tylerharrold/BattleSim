using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public static class CombatRollRules
{
    public const decimal MinimumLuckRerollChance = 0.02m;
    public const decimal LuckRerollChancePerPoint = 0.0065m;
    public const decimal MaximumLuckRerollChance = 0.50m;

    public const decimal MinimumCriticalChance = 0.03m;
    public const decimal CriticalChancePerLuckPoint = 0.004m;
    public const decimal MaximumCriticalChance = 0.40m;

    public const decimal CriticalDamageScalar = 0.75m;

    public static decimal GetLuckRerollChance(int luck)
    {
        return Clamp(MinimumLuckRerollChance + luck * LuckRerollChancePerPoint, MinimumLuckRerollChance, MaximumLuckRerollChance);
    }

    public static decimal GetCriticalChance(int luck)
    {
        return Clamp(MinimumCriticalChance + luck * CriticalChancePerLuckPoint, MinimumCriticalChance, MaximumCriticalChance);
    }

    public static int GetScalingStat(Stats stats, CombatStat stat)
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

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }
}

using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public sealed record ActionStatScaling(
    CombatStat Stat,
    decimal MinPercentPerStatPoint,
    decimal MaxPercentPerStatPoint)
{
    public decimal RollMultiplier(int statValue, Random random)
    {
        if (MinPercentPerStatPoint > MaxPercentPerStatPoint)
        {
            throw new InvalidOperationException("Minimum scaling percent cannot be greater than maximum scaling percent.");
        }

        var range = MaxPercentPerStatPoint - MinPercentPerStatPoint;
        var rolledPercentPerPoint = MinPercentPerStatPoint + range * (decimal)random.NextDouble();

        return 1m + statValue * rolledPercentPerPoint;
    }
}

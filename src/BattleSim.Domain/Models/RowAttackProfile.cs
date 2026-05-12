using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public sealed record RowAttackProfile(int Front, int Middle, int Back)
{
    public int GetAttackCount(FormationRank rank)
    {
        return rank switch
        {
            FormationRank.Front => Front,
            FormationRank.Middle => Middle,
            FormationRank.Back => Back,
            _ => throw new ArgumentOutOfRangeException(nameof(rank))
        };
    }
}

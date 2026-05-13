using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public sealed record RowActionProfile(
    IReadOnlyList<BattleActionDefinition> Front,
    IReadOnlyList<BattleActionDefinition> Middle,
    IReadOnlyList<BattleActionDefinition> Back)
{
    public IReadOnlyList<BattleActionDefinition> GetActions(FormationRank rank)
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

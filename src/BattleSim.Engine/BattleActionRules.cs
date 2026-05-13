using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public static class BattleActionRules
{
    public static IReadOnlyList<BattleActionDefinition> GetBattleActions(Troop troop, BattleSide side)
    {
        var rank = GetFormationRank(troop.Position, side);

        return troop.ClassDefinition.ActionProfile.GetActions(rank);
    }

    public static FormationRank GetFormationRank(GridPosition position, BattleSide side)
    {
        return side switch
        {
            // The units face each other horizontally: Blue/left fronts to the right, Red/right fronts to the left.
            BattleSide.Left => position.Column switch
            {
                2 => FormationRank.Front,
                1 => FormationRank.Middle,
                0 => FormationRank.Back,
                _ => throw new ArgumentOutOfRangeException(nameof(position))
            },
            BattleSide.Right => position.Column switch
            {
                0 => FormationRank.Front,
                1 => FormationRank.Middle,
                2 => FormationRank.Back,
                _ => throw new ArgumentOutOfRangeException(nameof(position))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(side))
        };
    }
}

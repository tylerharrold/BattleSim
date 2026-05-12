using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public static class BattleAttackRules
{
    public static int GetBattleAttackLimit(Troop troop, BattleSide side)
    {
        var rank = GetFormationRank(troop.Position, side);

        return troop.TroopClass switch
        {
            TroopClass.Fighter => rank switch
            {
                FormationRank.Front => 3,
                FormationRank.Middle => 2,
                FormationRank.Back => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(rank))
            },
            TroopClass.Archer => rank switch
            {
                FormationRank.Front => 1,
                FormationRank.Middle => 2,
                FormationRank.Back => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(rank))
            },
            TroopClass.Cleric => rank switch
            {
                FormationRank.Front => 1,
                FormationRank.Middle => 2,
                FormationRank.Back => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(rank))
            },
            TroopClass.Wizard => rank switch
            {
                FormationRank.Front => 1,
                FormationRank.Middle => 3,
                FormationRank.Back => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(rank))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(troop.TroopClass))
        };
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

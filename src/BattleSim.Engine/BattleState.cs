using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed class BattleState
{
    public BattleState(Unit leftUnit, Unit rightUnit, int turnNumber = 1)
    {
        LeftUnit = leftUnit;
        RightUnit = rightUnit;
        TurnNumber = turnNumber;
    }

    public Unit LeftUnit { get; }

    public Unit RightUnit { get; }

    public int TurnNumber { get; }

    public bool IsComplete => LeftUnit.IsDefeated || RightUnit.IsDefeated;

    public BattleState AdvanceTurn() => new(LeftUnit.Clone(), RightUnit.Clone(), TurnNumber + 1);

    public static BattleState CreateDefault()
    {
        // The engine owns sample battle creation so the UI can reset without embedding combat setup rules.
        var left = new Unit("Blue Unit", new[]
        {
            new Troop("Blue Fighter", TroopClass.Fighter, new Stats(24, 7, 3, 4), new GridPosition(1, 0)),
            new Troop("Blue Archer", TroopClass.Archer, new Stats(18, 5, 1, 6), new GridPosition(0, 1)),
            new Troop("Blue Cleric", TroopClass.Cleric, new Stats(20, 4, 2, 3), new GridPosition(2, 1))
        });

        var right = new Unit("Red Unit", new[]
        {
            new Troop("Red Fighter", TroopClass.Fighter, new Stats(24, 7, 3, 4), new GridPosition(1, 2)),
            new Troop("Red Wizard", TroopClass.Wizard, new Stats(16, 8, 1, 5), new GridPosition(0, 1)),
            new Troop("Red Archer", TroopClass.Archer, new Stats(18, 5, 1, 6), new GridPosition(2, 1))
        });

        return new BattleState(left, right);
    }
}

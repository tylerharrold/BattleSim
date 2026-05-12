using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public static class BuiltInTroopClasses
{
    public static TroopClassDefinition Fighter { get; } = new(
        "fighter",
        "Fighter",
        new Stats(24, 7, 3, 4),
        new RowAttackProfile(Front: 3, Middle: 2, Back: 1));

    public static TroopClassDefinition Archer { get; } = new(
        "archer",
        "Archer",
        new Stats(18, 5, 1, 6),
        new RowAttackProfile(Front: 1, Middle: 2, Back: 3));

    public static TroopClassDefinition Cleric { get; } = new(
        "cleric",
        "Cleric",
        new Stats(20, 4, 2, 3),
        new RowAttackProfile(Front: 1, Middle: 2, Back: 2));

    public static TroopClassDefinition Wizard { get; } = new(
        "wizard",
        "Wizard",
        new Stats(16, 8, 1, 5),
        new RowAttackProfile(Front: 1, Middle: 3, Back: 2));

    public static TroopClassDefinition FromLegacyEnum(TroopClass troopClass)
    {
        return troopClass switch
        {
            TroopClass.Fighter => Fighter,
            TroopClass.Archer => Archer,
            TroopClass.Cleric => Cleric,
            TroopClass.Wizard => Wizard,
            _ => throw new ArgumentOutOfRangeException(nameof(troopClass))
        };
    }
}

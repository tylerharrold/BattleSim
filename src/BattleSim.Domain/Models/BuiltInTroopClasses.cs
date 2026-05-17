using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public static class BuiltInTroopClasses
{
    public static TroopClassDefinition Fighter { get; } = new(
        "fighter",
        "Fighter",
        new Stats(MaxHitPoints: 24, Strength: 7, Defense: 3, Speed: 4, Faith: 2, Wisdom: 2, Dexterity: 4, Luck: 5),
        new RowActionProfile(
            Front: Repeat(BuiltInBattleActions.Slash, 3),
            Middle: Repeat(BuiltInBattleActions.Slash, 2),
            Back: Repeat(BuiltInBattleActions.Slash, 1)),
        "avares://BattleSim.App/Assets/Images/Portraits/fighter.png");

    public static TroopClassDefinition Archer { get; } = new(
        "archer",
        "Archer",
        new Stats(MaxHitPoints: 18, Strength: 4, Defense: 1, Speed: 6, Faith: 2, Wisdom: 3, Dexterity: 8, Luck: 8),
        new RowActionProfile(
            Front: Repeat(BuiltInBattleActions.BowShot, 1),
            Middle: Repeat(BuiltInBattleActions.BowShot, 2),
            Back: Repeat(BuiltInBattleActions.BowShot, 3)),
        "avares://BattleSim.App/Assets/Images/Portraits/archer.png");

    public static TroopClassDefinition Cleric { get; } = new(
        "cleric",
        "Cleric",
        new Stats(MaxHitPoints: 20, Strength: 4, Defense: 2, Speed: 3, Faith: 8, Wisdom: 5, Dexterity: 3, Luck: 6),
        new RowActionProfile(
            Front: Repeat(BuiltInBattleActions.StaffBonk, 1),
            Middle: Repeat(BuiltInBattleActions.Heal, 2),
            Back: Repeat(BuiltInBattleActions.Heal, 2)),
        "avares://BattleSim.App/Assets/Images/Portraits/cleric.png");

    public static TroopClassDefinition Wizard { get; } = new(
        "wizard",
        "Wizard",
        new Stats(MaxHitPoints: 16, Strength: 3, Defense: 1, Speed: 5, Faith: 4, Wisdom: 8, Dexterity: 3, Luck: 4),
        new RowActionProfile(
            Front: Repeat(BuiltInBattleActions.StaffBonk, 1),
            Middle: Repeat(BuiltInBattleActions.Firebolt, 3),
            Back: Repeat(BuiltInBattleActions.Firebolt, 2)),
        "avares://BattleSim.App/Assets/Images/Portraits/wizard.png");

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

    private static IReadOnlyList<BattleActionDefinition> Repeat(BattleActionDefinition action, int count)
    {
        return Enumerable.Repeat(action, count).ToArray();
    }
}

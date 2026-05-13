using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public static class BuiltInBattleActions
{
    public static BattleActionDefinition Slash { get; } = new(
        "slash",
        "Slash",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        "melee",
        1.0m);

    public static BattleActionDefinition BowShot { get; } = new(
        "bow_shot",
        "Bow Shot",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        "ranged",
        1.0m);

    public static BattleActionDefinition StaffBonk { get; } = new(
        "staff_bonk",
        "Staff Bonk",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        "melee",
        0.5m);

    public static BattleActionDefinition Firebolt { get; } = new(
        "firebolt",
        "Firebolt",
        ActionKind.MagicalDamage,
        TargetSide.Enemy,
        "ranged",
        1.0m);

    public static BattleActionDefinition Heal { get; } = new(
        "heal",
        "Heal",
        ActionKind.Heal,
        TargetSide.Ally,
        "most_damaged_ally",
        1.0m);
}

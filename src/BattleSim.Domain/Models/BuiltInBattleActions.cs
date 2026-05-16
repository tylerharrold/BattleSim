using BattleSim.Domain.Enums;
using BattleSim.Domain.Targeting;

namespace BattleSim.Domain.Models;

public static class BuiltInBattleActions
{
    private static readonly ITargetingRule Melee = new MeleeTargetingRule();
    private static readonly ITargetingRule Ranged = new RangedTargetingRule();
    private static readonly ITargetingRule MostDamagedAlly = new MostDamagedAllyTargetingRule();

    public static BattleActionDefinition Slash { get; } = new(
        "slash",
        "Slash",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        Melee,
        1.0m);

    public static BattleActionDefinition BowShot { get; } = new(
        "bow_shot",
        "Bow Shot",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        Ranged,
        1.0m);

    public static BattleActionDefinition StaffBonk { get; } = new(
        "staff_bonk",
        "Staff Bonk",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        Melee,
        0.5m);

    public static BattleActionDefinition Firebolt { get; } = new(
        "firebolt",
        "Firebolt",
        ActionKind.MagicalDamage,
        TargetSide.Enemy,
        Ranged,
        1.0m);

    public static BattleActionDefinition Heal { get; } = new(
        "heal",
        "Heal",
        ActionKind.Heal,
        TargetSide.Ally,
        MostDamagedAlly,
        1.0m);
}

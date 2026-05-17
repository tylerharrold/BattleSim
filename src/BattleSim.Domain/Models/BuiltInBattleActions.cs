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
        BasePower: 5,
        Scaling: new ActionStatScaling(CombatStat.Strength, 0.08m, 0.12m),
        Accuracy: 0.95m,
        CanCrit: true);

    public static BattleActionDefinition BowShot { get; } = new(
        "bow_shot",
        "Bow Shot",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        Ranged,
        BasePower: 4,
        Scaling: new ActionStatScaling(CombatStat.Dexterity, 0.08m, 0.12m),
        Accuracy: 0.9m,
        CanCrit: true);

    public static BattleActionDefinition StaffBonk { get; } = new(
        "staff_bonk",
        "Staff Bonk",
        ActionKind.PhysicalDamage,
        TargetSide.Enemy,
        Melee,
        BasePower: 3,
        Scaling: new ActionStatScaling(CombatStat.Strength, 0.05m, 0.08m),
        Accuracy: 0.95m,
        CanCrit: true);

    public static BattleActionDefinition Firebolt { get; } = new(
        "firebolt",
        "Firebolt",
        ActionKind.MagicalDamage,
        TargetSide.Enemy,
        Ranged,
        BasePower: 5,
        Scaling: new ActionStatScaling(CombatStat.Wisdom, 0.0875m, 0.125m),
        Accuracy: 0.85m,
        CanCrit: false);

    public static BattleActionDefinition Heal { get; } = new(
        "heal",
        "Heal",
        ActionKind.Heal,
        TargetSide.Ally,
        MostDamagedAlly,
        BasePower: 4,
        Scaling: new ActionStatScaling(CombatStat.Faith, 0.08m, 0.12m),
        Accuracy: 1.0m,
        CanCrit: false);
}

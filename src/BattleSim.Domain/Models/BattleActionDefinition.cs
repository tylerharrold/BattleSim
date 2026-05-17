using BattleSim.Domain.Enums;
using BattleSim.Domain.Targeting;

namespace BattleSim.Domain.Models;

public sealed record BattleActionDefinition(
    string Id,
    string DisplayName,
    ActionKind ActionKind,
    TargetSide TargetSide,
    ITargetingRule TargetingRule,
    int BasePower,
    ActionStatScaling Scaling,
    decimal Accuracy);

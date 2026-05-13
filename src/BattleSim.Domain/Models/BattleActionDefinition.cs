using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public sealed record BattleActionDefinition(
    string Id,
    string DisplayName,
    ActionKind ActionKind,
    TargetSide TargetSide,
    string TargetingRuleId,
    decimal Power);

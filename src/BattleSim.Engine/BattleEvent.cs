using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed record BattleEvent(
    string Description,
    string? ActorName = null,
    string? TargetName = null,
    int Damage = 0,
    BattleSide? ActorSide = null,
    GridPosition? ActorPosition = null,
    BattleSide? TargetSide = null,
    GridPosition? TargetPosition = null,
    BattleEventIntent Intent = BattleEventIntent.Neutral);

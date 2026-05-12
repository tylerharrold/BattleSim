using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed record BattleEvent(
    string Description,
    string? ActorName = null,
    string? TargetName = null,
    int Damage = 0,
    GridPosition? ActorPosition = null,
    GridPosition? TargetPosition = null);

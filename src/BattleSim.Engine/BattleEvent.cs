using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed record BattleEvent(
    string ActorName,
    string TargetName,
    int Damage,
    string Description,
    GridPosition ActorPosition,
    GridPosition TargetPosition);

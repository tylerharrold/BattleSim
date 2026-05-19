using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed record BattleActionTargetResult(
    Troop Target,
    bool DidHit,
    bool WasLuckReroll,
    bool WasCritical,
    int DamageDealt,
    int HealingDone,
    bool WasDefeated,
    BattleEvent Event);

using BattleSim.Domain.Models;

namespace BattleSim.Domain.Targeting;

public sealed record TargetingContext(
    Troop Attacker,
    Unit Allies,
    Unit Enemies);

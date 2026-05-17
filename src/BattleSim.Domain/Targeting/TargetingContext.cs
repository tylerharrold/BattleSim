using BattleSim.Domain.Models;
using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Targeting;

public sealed record TargetingContext(
    Troop Attacker,
    Unit Allies,
    Unit Enemies,
    FormationOrientation AlliesOrientation,
    FormationOrientation EnemiesOrientation,
    Random Random);

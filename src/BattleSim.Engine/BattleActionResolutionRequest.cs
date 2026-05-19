using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed record BattleActionResolutionRequest(
    Troop Attacker,
    BattleActionDefinition Action,
    IReadOnlyList<Troop> Targets,
    BattleSide AttackerSide,
    BattleSide TargetSide);

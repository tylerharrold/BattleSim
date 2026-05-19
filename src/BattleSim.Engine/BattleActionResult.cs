using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed record BattleActionResult(
    BattleActionOutcome Outcome,
    Troop Attacker,
    BattleActionDefinition Action,
    IReadOnlyList<BattleActionTargetResult> TargetResults,
    IReadOnlyList<BattleEvent> Events)
{
    public bool HasTargets => TargetResults.Count > 0;
}

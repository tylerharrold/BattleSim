namespace BattleSim.Engine;

public sealed record BattleStepResult(BattleState State, IReadOnlyList<BattleEvent> Events);

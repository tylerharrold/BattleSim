namespace BattleSim.Engine;

public sealed record BattlePlan(
    IReadOnlyList<BattleSide> UnitOrder,
    IReadOnlyDictionary<BattleSide, IReadOnlyList<string>> TroopOrders);

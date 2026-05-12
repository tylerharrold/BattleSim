using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed class BattleState
{
    public BattleState(
        Unit leftUnit,
        Unit rightUnit,
        BattlePlan plan,
        int roundNumber = 1,
        int unitOrderIndex = 0,
        int troopOrderIndex = 0,
        bool hasRoundStarted = false)
    {
        LeftUnit = leftUnit;
        RightUnit = rightUnit;
        Plan = plan;
        RoundNumber = roundNumber;
        UnitOrderIndex = unitOrderIndex;
        TroopOrderIndex = troopOrderIndex;
        HasRoundStarted = hasRoundStarted;
    }

    public Unit LeftUnit { get; }

    public Unit RightUnit { get; }

    public BattlePlan Plan { get; }

    public int RoundNumber { get; }

    public int UnitOrderIndex { get; }

    public int TroopOrderIndex { get; }

    public bool HasRoundStarted { get; }

    public BattleSide CurrentSide => Plan.UnitOrder[UnitOrderIndex];

    public bool IsComplete => LeftUnit.IsDefeated || RightUnit.IsDefeated;

    public BattleState CloneForProgress()
    {
        return new BattleState(
            LeftUnit.Clone(),
            RightUnit.Clone(),
            Plan,
            RoundNumber,
            UnitOrderIndex,
            TroopOrderIndex,
            HasRoundStarted);
    }

    public BattleState WithProgress(int roundNumber, int unitOrderIndex, int troopOrderIndex, bool hasRoundStarted)
    {
        return new BattleState(
            LeftUnit,
            RightUnit,
            Plan,
            roundNumber,
            unitOrderIndex,
            troopOrderIndex,
            hasRoundStarted);
    }

    public IReadOnlyList<BattleEvent> CreateSetupEvents()
    {
        var firstUnit = GetUnit(Plan.UnitOrder[0]).Name;
        var secondUnit = GetUnit(Plan.UnitOrder[1]).Name;

        return new[]
        {
            new BattleEvent($"Unit order: {firstUnit}, then {secondUnit}."),
            new BattleEvent($"{LeftUnit.Name} troop order: {FormatTroopOrder(BattleSide.Left)}."),
            new BattleEvent($"{RightUnit.Name} troop order: {FormatTroopOrder(BattleSide.Right)}.")
        };
    }

    public Unit GetUnit(BattleSide side) => side == BattleSide.Left ? LeftUnit : RightUnit;

    public Unit GetOpponent(BattleSide side) => side == BattleSide.Left ? RightUnit : LeftUnit;

    public static BattleState CreateDefault(int? seed = null)
    {
        // The engine owns sample battle creation so the UI can reset without embedding combat setup rules.
        var left = new Unit("Blue Unit", new[]
        {
            new Troop("Blue Fighter", TroopClass.Fighter, new Stats(24, 7, 3, 4), new GridPosition(1, 0)),
            new Troop("Blue Archer", TroopClass.Archer, new Stats(18, 5, 1, 6), new GridPosition(0, 1)),
            new Troop("Blue Cleric", TroopClass.Cleric, new Stats(20, 4, 2, 3), new GridPosition(2, 1))
        });

        var right = new Unit("Red Unit", new[]
        {
            new Troop("Red Fighter", TroopClass.Fighter, new Stats(24, 7, 3, 4), new GridPosition(1, 2)),
            new Troop("Red Wizard", TroopClass.Wizard, new Stats(16, 8, 1, 5), new GridPosition(0, 1)),
            new Troop("Red Archer", TroopClass.Archer, new Stats(18, 5, 1, 6), new GridPosition(2, 1))
        });

        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var plan = CreateBattlePlan(left, right, random);

        return new BattleState(left, right, plan);
    }

    private static BattlePlan CreateBattlePlan(Unit left, Unit right, Random random)
    {
        // These random choices happen once at battle creation so every round uses the same flow.
        var unitOrder = random.Next(2) == 0
            ? new[] { BattleSide.Left, BattleSide.Right }
            : new[] { BattleSide.Right, BattleSide.Left };

        var troopOrders = new Dictionary<BattleSide, IReadOnlyList<string>>
        {
            [BattleSide.Left] = Shuffle(left.Troops.Select(troop => troop.Name), random),
            [BattleSide.Right] = Shuffle(right.Troops.Select(troop => troop.Name), random)
        };

        return new BattlePlan(unitOrder, troopOrders);
    }

    private static IReadOnlyList<string> Shuffle(IEnumerable<string> names, Random random)
    {
        var shuffled = names.ToList();

        for (var index = shuffled.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }

        return shuffled;
    }

    private string FormatTroopOrder(BattleSide side)
    {
        return string.Join(", ", Plan.TroopOrders[side]);
    }
}

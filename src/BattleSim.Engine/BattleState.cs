using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed class BattleState
{
    private static readonly UnitFactory DefaultUnitFactory = new(BuiltInTroopClasses.ById);

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

    public bool IsComplete => !AllTroops.Any(troop => troop.CanAttack);

    public IEnumerable<Troop> AllTroops => LeftUnit.Troops.Concat(RightUnit.Troops);

    public bool HasBattleStarted =>
        HasRoundStarted ||
        RoundNumber != 1 ||
        UnitOrderIndex != 0 ||
        TroopOrderIndex != 0 ||
        AllTroops.Any(troop => troop.RemainingBattleAttacks != troop.MaxBattleAttacks);

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

    public BattleState RotateFormationClockwise(BattleSide side)
    {
        if (HasBattleStarted)
        {
            throw new InvalidOperationException("Formations can only be rotated before battle starts.");
        }

        var rotatedUnit = RotateUnitClockwise(GetUnit(side));
        ApplyBattleAttackLimits(rotatedUnit, side);

        return side == BattleSide.Left
            ? new BattleState(rotatedUnit, RightUnit, Plan)
            : new BattleState(LeftUnit, rotatedUnit, Plan);
    }

    public BattleState MoveTroop(BattleSide side, string troopName, GridPosition destination)
    {
        if (HasBattleStarted)
        {
            throw new InvalidOperationException("Troops can only be moved before battle starts.");
        }

        if (!destination.IsInFormation)
        {
            throw new ArgumentOutOfRangeException(nameof(destination), "Troop positions must fit inside a 3x3 formation.");
        }

        var unit = GetUnit(side);
        var troop = unit.Troops.FirstOrDefault(candidate => candidate.Name == troopName)
            ?? throw new ArgumentException($"Could not find troop '{troopName}'.", nameof(troopName));

        if (troop.Position == destination)
        {
            return this;
        }

        if (unit.Troops.Any(candidate => candidate.Position == destination))
        {
            throw new InvalidOperationException("Troops can only move into empty formation slots.");
        }

        var movedUnit = new Unit(
            unit.Name,
            unit.Troops.Select(candidate =>
                candidate.Name == troopName ? candidate.CloneAtPosition(destination) : candidate.Clone()),
            unit.LeaderName,
            unit.TargetingPreference);

        ApplyBattleAttackLimits(movedUnit, side);

        return side == BattleSide.Left
            ? new BattleState(movedUnit, RightUnit, Plan)
            : new BattleState(LeftUnit, movedUnit, Plan);
    }

    public IReadOnlyList<BattleEvent> CreateSetupEvents()
    {
        var firstUnit = GetUnit(Plan.UnitOrder[0]).Name;
        var secondUnit = GetUnit(Plan.UnitOrder[1]).Name;

        return new[]
        {
            new BattleEvent($"Unit order: {firstUnit}, then {secondUnit}."),
            new BattleEvent($"{LeftUnit.Name} troop order: {FormatTroopOrder(BattleSide.Left)}."),
            new BattleEvent($"{RightUnit.Name} troop order: {FormatTroopOrder(BattleSide.Right)}."),
            new BattleEvent($"{LeftUnit.Name} action counts: {FormatAttackCounts(LeftUnit)}."),
            new BattleEvent($"{RightUnit.Name} action counts: {FormatAttackCounts(RightUnit)}.")
        };
    }

    public Unit GetUnit(BattleSide side) => side == BattleSide.Left ? LeftUnit : RightUnit;

    public Unit GetOpponent(BattleSide side) => side == BattleSide.Left ? RightUnit : LeftUnit;

    public BattleState SetTargetingPreference(BattleSide side, TargetingPreference targetingPreference)
    {
        return side == BattleSide.Left
            ? new BattleState(LeftUnit.WithTargetingPreference(targetingPreference), RightUnit, Plan, RoundNumber, UnitOrderIndex, TroopOrderIndex, HasRoundStarted)
            : new BattleState(LeftUnit, RightUnit.WithTargetingPreference(targetingPreference), Plan, RoundNumber, UnitOrderIndex, TroopOrderIndex, HasRoundStarted);
    }

    public BattleState ReplaceUnitFromTemplate(BattleSide side, UnitTemplate template, int? seed = null)
    {
        if (HasBattleStarted)
        {
            throw new InvalidOperationException("Unit templates can only be changed before battle starts.");
        }

        var replacementUnit = DefaultUnitFactory.Create(template, side)
            .WithTargetingPreference(GetUnit(side).TargetingPreference);
        ApplyBattleAttackLimits(replacementUnit, side);

        var left = side == BattleSide.Left ? replacementUnit : LeftUnit.Clone();
        var right = side == BattleSide.Right ? replacementUnit : RightUnit.Clone();
        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;

        return new BattleState(left, right, CreateBattlePlan(left, right, random));
    }

    public static BattleState CreateDefault(int? seed = null)
    {
        return CreateFromTemplates(CreateDefaultLeftTemplate(), CreateDefaultRightTemplate(), seed);
    }

    public static BattleState CreateFromTemplates(UnitTemplate leftTemplate, UnitTemplate rightTemplate, int? seed = null)
    {
        // The engine owns runtime battle creation so the UI can reset without embedding combat setup rules.
        var left = DefaultUnitFactory.Create(leftTemplate, BattleSide.Left);
        var right = DefaultUnitFactory.Create(rightTemplate, BattleSide.Right);

        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        ApplyBattleAttackLimits(left, BattleSide.Left);
        ApplyBattleAttackLimits(right, BattleSide.Right);
        var plan = CreateBattlePlan(left, right, random);

        return new BattleState(left, right, plan);
    }

    public static UnitTemplate CreateDefaultLeftTemplate()
    {
        return new UnitTemplate
        {
            Id = "blue-balanced-test",
            Name = "Blue Unit",
            Troops =
            [
                new UnitTemplateTroop
                {
                    SlotId = "fighter-1",
                    Name = "Blue Fighter",
                    TroopClassId = BuiltInTroopClasses.Fighter.Id,
                    Row = 1,
                    Column = 2
                },
                new UnitTemplateTroop
                {
                    SlotId = "archer-1",
                    Name = "Blue Archer",
                    TroopClassId = BuiltInTroopClasses.Archer.Id,
                    Row = 0,
                    Column = 1
                },
                new UnitTemplateTroop
                {
                    SlotId = "cleric-leader",
                    Name = "Blue Cleric",
                    TroopClassId = BuiltInTroopClasses.Cleric.Id,
                    Row = 2,
                    Column = 1,
                    IsLeader = true
                }
            ]
        };
    }

    public static UnitTemplate CreateDefaultRightTemplate()
    {
        return new UnitTemplate
        {
            Id = "red-balanced-test",
            Name = "Red Unit",
            Troops =
            [
                new UnitTemplateTroop
                {
                    SlotId = "fighter-leader",
                    Name = "Red Fighter",
                    TroopClassId = BuiltInTroopClasses.Fighter.Id,
                    Row = 1,
                    Column = 2,
                    IsLeader = true
                },
                new UnitTemplateTroop
                {
                    SlotId = "wizard-1",
                    Name = "Red Wizard",
                    TroopClassId = BuiltInTroopClasses.Wizard.Id,
                    Row = 2,
                    Column = 1
                },
                new UnitTemplateTroop
                {
                    SlotId = "archer-1",
                    Name = "Red Archer",
                    TroopClassId = BuiltInTroopClasses.Archer.Id,
                    Row = 0,
                    Column = 1
                }
            ]
        };
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

    private static string FormatAttackCounts(Unit unit)
    {
        return string.Join(", ", unit.Troops.Select(troop => $"{troop.Name} {troop.RemainingBattleAttacks}"));
    }

    private static Unit RotateUnitClockwise(Unit unit)
    {
        return new Unit(
            unit.Name,
            unit.Troops.Select(troop => troop.CloneAtPosition(RotatePositionClockwise(troop.Position))),
            unit.LeaderName,
            unit.TargetingPreference);
    }

    private static GridPosition RotatePositionClockwise(GridPosition position)
    {
        return new GridPosition(position.Column, 2 - position.Row);
    }

    private static void ApplyBattleAttackLimits(Unit unit, BattleSide side)
    {
        foreach (var troop in unit.Troops)
        {
            troop.SetBattleAttackLimit(BattleActionRules.GetBattleActions(troop, side).Count);
        }
    }
}

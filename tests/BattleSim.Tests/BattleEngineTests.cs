using BattleSim.Engine;
using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;
using Xunit;

namespace BattleSim.Tests;

public sealed class BattleEngineTests
{
    [Fact]
    public void RunNextAttack_RunsOnlyNextScheduledTroopAttack()
    {
        var engine = new BattleEngine();
        var state = BattleState.CreateDefault(seed: 1);

        var result = engine.RunNextAttack(state);

        var firstSide = state.Plan.UnitOrder[0];
        var firstAttacker = state.Plan.TroopOrders[firstSide][0];

        Assert.Equal(1, result.State.RoundNumber);
        Assert.Equal(firstSide, result.State.CurrentSide);
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("Round 1 begins.", result.Events[0].Description);
        Assert.Equal($"{state.GetUnit(firstSide).Name} attacks.", result.Events[1].Description);
        Assert.Equal(firstAttacker, result.Events[2].ActorName);
    }

    [Fact]
    public void RunNextTurn_RunsRemainingAttacksForCurrentUnitOnly()
    {
        var engine = new BattleEngine();
        var state = BattleState.CreateDefault(seed: 1);

        var result = engine.RunNextTurn(state);

        var firstSide = state.Plan.UnitOrder[0];
        var secondSide = state.Plan.UnitOrder[1];
        var actualAttackers = result.Events
            .Where(battleEvent => battleEvent.ActorName is not null)
            .Select(battleEvent => battleEvent.ActorName!)
            .ToArray();

        Assert.Equal(1, result.State.RoundNumber);
        Assert.Equal(secondSide, result.State.CurrentSide);
        Assert.Equal(state.Plan.TroopOrders[firstSide], actualAttackers);
    }

    [Fact]
    public void RunOneRound_UsesBattlePlanAndAdvancesRound()
    {
        var engine = new BattleEngine();
        var state = BattleState.CreateDefault(seed: 1);

        var result = engine.RunOneRound(state);

        Assert.Equal(2, result.State.RoundNumber);
        Assert.Equal(9, result.Events.Count);
        Assert.Equal("Round 1 begins.", result.Events[0].Description);
        Assert.Equal($"{state.GetUnit(state.Plan.UnitOrder[0]).Name} attacks.", result.Events[1].Description);
        Assert.Equal($"{state.GetUnit(state.Plan.UnitOrder[1]).Name} attacks.", result.Events[5].Description);

        var expectedAttackers = state.Plan.UnitOrder
            .SelectMany(side => state.Plan.TroopOrders[side])
            .ToArray();
        var actualAttackers = result.Events
            .Where(battleEvent => battleEvent.ActorName is not null)
            .Select(battleEvent => battleEvent.ActorName!)
            .ToArray();

        Assert.Equal(expectedAttackers, actualAttackers);
    }

    [Fact]
    public void CreateDefault_IncludesSetupEventsForBattleLog()
    {
        var state = BattleState.CreateDefault(seed: 1);

        var setupEvents = state.CreateSetupEvents();

        Assert.Equal(5, setupEvents.Count);
        Assert.StartsWith("Unit order:", setupEvents[0].Description);
        Assert.StartsWith("Blue Unit troop order:", setupEvents[1].Description);
        Assert.StartsWith("Red Unit troop order:", setupEvents[2].Description);
        Assert.StartsWith("Blue Unit attack counts:", setupEvents[3].Description);
        Assert.StartsWith("Red Unit attack counts:", setupEvents[4].Description);
    }

    [Fact]
    public void FighterBattleAttackLimit_UsesDefinitionProfileByRelativeFormationRank()
    {
        var leftBack = new Troop("Left Back Fighter", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        var leftMiddle = new Troop("Left Middle Fighter", BuiltInTroopClasses.Fighter, new GridPosition(1, 1));
        var leftFront = new Troop("Left Front Fighter", BuiltInTroopClasses.Fighter, new GridPosition(1, 2));

        Assert.Equal(1, BattleAttackRules.GetBattleAttackLimit(leftBack, BattleSide.Left));
        Assert.Equal(2, BattleAttackRules.GetBattleAttackLimit(leftMiddle, BattleSide.Left));
        Assert.Equal(3, BattleAttackRules.GetBattleAttackLimit(leftFront, BattleSide.Left));
    }

    [Fact]
    public void ArcherBattleAttackLimit_UsesDefinitionProfileByRelativeFormationRank()
    {
        var rightFront = new Troop("Right Front Archer", BuiltInTroopClasses.Archer, new GridPosition(1, 0));
        var rightMiddle = new Troop("Right Middle Archer", BuiltInTroopClasses.Archer, new GridPosition(1, 1));
        var rightBack = new Troop("Right Back Archer", BuiltInTroopClasses.Archer, new GridPosition(1, 2));

        Assert.Equal(1, BattleAttackRules.GetBattleAttackLimit(rightFront, BattleSide.Right));
        Assert.Equal(2, BattleAttackRules.GetBattleAttackLimit(rightMiddle, BattleSide.Right));
        Assert.Equal(3, BattleAttackRules.GetBattleAttackLimit(rightBack, BattleSide.Right));
    }

    [Fact]
    public void BattleAttackRules_DoesNotContainClassSpecificSwitchLogic()
    {
        var sourcePath = FindRepoFile("src/BattleSim.Engine/BattleAttackRules.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("TroopClass.", source);
        Assert.DoesNotContain("BuiltInTroopClasses.", source);
    }

    [Fact]
    public void RunNextAttack_DecrementsRemainingBattleAttacksAndLogsCount()
    {
        var engine = new BattleEngine();
        var state = BattleState.CreateDefault(seed: 1);

        var result = engine.RunNextAttack(state);
        var attackEvent = result.Events.Last(battleEvent => battleEvent.ActorName is not null);
        var attacker = result.State.AllTroops.Single(troop => troop.Name == attackEvent.ActorName);

        Assert.Contains($"{attacker.Name} has {attacker.RemainingBattleAttacks} attacks left.", attackEvent.Description);
        Assert.Equal(attacker.MaxBattleAttacks - 1, attacker.RemainingBattleAttacks);
    }

    [Fact]
    public void BattleCompletesWhenEveryLivingTroopExhaustsBattleAttacks()
    {
        var engine = new BattleEngine();
        var state = BattleState.CreateDefault(seed: 1);

        while (!state.IsComplete)
        {
            state = engine.RunOneRound(state).State;
        }

        Assert.All(state.AllTroops.Where(troop => !troop.IsDefeated), troop => Assert.Equal(0, troop.RemainingBattleAttacks));
    }

    [Fact]
    public void RotateFormationClockwise_RecalculatesRelativeAttackCounts()
    {
        var state = BattleState.CreateDefault(seed: 1);
        var initialBlueFighter = state.LeftUnit.Troops.Single(troop => troop.Name == "Blue Fighter");

        state = state.RotateFormationClockwise(BattleSide.Left);
        state = state.RotateFormationClockwise(BattleSide.Left);

        var rotatedBlueFighter = state.LeftUnit.Troops.Single(troop => troop.Name == "Blue Fighter");

        Assert.Equal(1, initialBlueFighter.MaxBattleAttacks);
        Assert.Equal(new GridPosition(1, 2), rotatedBlueFighter.Position);
        Assert.Equal(3, rotatedBlueFighter.MaxBattleAttacks);
        Assert.Equal(3, rotatedBlueFighter.RemainingBattleAttacks);
    }

    [Fact]
    public void RotateFormationClockwise_IsBlockedAfterBattleStarts()
    {
        var engine = new BattleEngine();
        var state = engine.RunNextAttack(BattleState.CreateDefault(seed: 1)).State;

        Assert.Throws<InvalidOperationException>(() => state.RotateFormationClockwise(BattleSide.Left));
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }
}

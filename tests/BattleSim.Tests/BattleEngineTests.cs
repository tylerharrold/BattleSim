using BattleSim.Engine;
using BattleSim.Domain.Enums;
using BattleSim.Domain.Models;
using BattleSim.Domain.Targeting;
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
        Assert.StartsWith("Blue Unit action counts:", setupEvents[3].Description);
        Assert.StartsWith("Red Unit action counts:", setupEvents[4].Description);
    }

    [Fact]
    public void FighterBattleActions_UseDefinitionProfileByRelativeFormationRank()
    {
        var leftBack = new Troop("Left Back Fighter", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        var leftFront = new Troop("Left Front Fighter", BuiltInTroopClasses.Fighter, new GridPosition(1, 2));

        Assert.Equal(new[] { "Slash", "Slash", "Slash" }, GetActionNames(leftFront, BattleSide.Left));
        Assert.Equal(new[] { "Slash" }, GetActionNames(leftBack, BattleSide.Left));
    }

    [Fact]
    public void ArcherBattleActions_UseDefinitionProfileByRelativeFormationRank()
    {
        var rightBack = new Troop("Right Back Archer", BuiltInTroopClasses.Archer, new GridPosition(1, 2));

        Assert.Equal(new[] { "Bow Shot", "Bow Shot", "Bow Shot" }, GetActionNames(rightBack, BattleSide.Right));
    }

    [Fact]
    public void WizardBattleActions_UseDefinitionProfileByRelativeFormationRank()
    {
        var rightFront = new Troop("Right Front Wizard", BuiltInTroopClasses.Wizard, new GridPosition(1, 0));
        var rightMiddle = new Troop("Right Middle Wizard", BuiltInTroopClasses.Wizard, new GridPosition(1, 1));

        Assert.Equal(new[] { "Staff Bonk" }, GetActionNames(rightFront, BattleSide.Right));
        Assert.Equal(new[] { "Firebolt", "Firebolt", "Firebolt" }, GetActionNames(rightMiddle, BattleSide.Right));
    }

    [Fact]
    public void ClericBattleActions_UseStaffBonkOnlyFromFrontRowOtherwiseHeal()
    {
        var leftFront = new Troop("Left Front Cleric", BuiltInTroopClasses.Cleric, new GridPosition(1, 2));
        var leftMiddle = new Troop("Left Middle Cleric", BuiltInTroopClasses.Cleric, new GridPosition(1, 1));
        var leftBack = new Troop("Left Back Cleric", BuiltInTroopClasses.Cleric, new GridPosition(1, 0));

        Assert.Equal(new[] { "Staff Bonk" }, GetActionNames(leftFront, BattleSide.Left));
        Assert.Equal(new[] { "Heal", "Heal" }, GetActionNames(leftMiddle, BattleSide.Left));
        Assert.Equal(new[] { "Heal", "Heal" }, GetActionNames(leftBack, BattleSide.Left));
    }

    [Fact]
    public void RowActionProfile_PreservesActionOrdering()
    {
        var profile = new RowActionProfile(
            Front: new[] { BuiltInBattleActions.Slash, BuiltInBattleActions.Firebolt, BuiltInBattleActions.BowShot },
            Middle: Array.Empty<BattleActionDefinition>(),
            Back: Array.Empty<BattleActionDefinition>());

        Assert.Equal(new[] { "slash", "firebolt", "bow_shot" }, profile.GetActions(FormationRank.Front).Select(action => action.Id));
    }

    [Fact]
    public void BattleActionRules_DoesNotContainClassSpecificSwitchLogic()
    {
        var sourcePath = FindRepoFile("src/BattleSim.Engine/BattleActionRules.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("TroopClass.", source);
        Assert.DoesNotContain("BuiltInTroopClasses.", source);
    }

    [Fact]
    public void BattleActionDefinition_UsesTargetingRuleObject()
    {
        Assert.IsType<MeleeTargetingRule>(BuiltInBattleActions.Slash.TargetingRule);
        Assert.IsType<RangedTargetingRule>(BuiltInBattleActions.BowShot.TargetingRule);
        Assert.IsType<MostDamagedAllyTargetingRule>(BuiltInBattleActions.Heal.TargetingRule);
    }

    [Fact]
    public void MeleeTargetingRule_SelectsValidEnemy()
    {
        var context = CreateTargetingContext();

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.True(selection.HasTargets);
        Assert.Equal("Enemy Front", selection.Targets.Single().Name);
    }

    [Fact]
    public void RangedTargetingRule_SelectsValidEnemy()
    {
        var context = CreateTargetingContext();

        var selection = BuiltInBattleActions.BowShot.TargetingRule.SelectTargets(context, BuiltInBattleActions.BowShot);

        Assert.True(selection.HasTargets);
        Assert.Equal("Enemy Front", selection.Targets.Single().Name);
    }

    [Fact]
    public void MostDamagedAllyTargetingRule_SelectsMostDamagedLivingAlly()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Cleric, new GridPosition(1, 1));
        var lightlyDamaged = new Troop("Lightly Damaged", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var heavilyDamaged = new Troop("Heavily Damaged", BuiltInTroopClasses.Fighter, new GridPosition(2, 0));
        lightlyDamaged.TakeDamage(2);
        heavilyDamaged.TakeDamage(8);
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker, lightlyDamaged, heavilyDamaged }),
            new Unit("Enemies", Array.Empty<Troop>()));

        var selection = BuiltInBattleActions.Heal.TargetingRule.SelectTargets(context, BuiltInBattleActions.Heal);

        Assert.True(selection.HasTargets);
        Assert.Equal("Heavily Damaged", selection.Targets.Single().Name);
    }

    [Fact]
    public void TargetingRule_ReturnsEmptySelectionWhenNoValidTargetsExist()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(1, 1));
        var defeatedEnemy = new Troop("Defeated Enemy", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        defeatedEnemy.TakeDamage(defeatedEnemy.Stats.MaxHitPoints);
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }),
            new Unit("Enemies", new[] { defeatedEnemy }));

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.False(selection.HasTargets);
        Assert.Empty(selection.Targets);
    }

    [Fact]
    public void RunNextAttack_DecrementsRemainingBattleAttacksAndLogsCount()
    {
        var engine = new BattleEngine();
        var state = BattleState.CreateDefault(seed: 1);

        var result = engine.RunNextAttack(state);
        var attackEvent = result.Events.Last(battleEvent => battleEvent.ActorName is not null);
        var attacker = result.State.AllTroops.Single(troop => troop.Name == attackEvent.ActorName);

        Assert.Contains(" uses ", attackEvent.Description);
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

    private static string[] GetActionNames(Troop troop, BattleSide side)
    {
        return BattleActionRules.GetBattleActions(troop, side)
            .Select(action => action.DisplayName)
            .ToArray();
    }

    private static TargetingContext CreateTargetingContext()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(1, 1));
        var backEnemy = new Troop("Enemy Back", BuiltInTroopClasses.Fighter, new GridPosition(2, 2));
        var frontEnemy = new Troop("Enemy Front", BuiltInTroopClasses.Fighter, new GridPosition(0, 1));

        return new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }),
            new Unit("Enemies", new[] { backEnemy, frontEnemy }));
    }
}

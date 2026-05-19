using BattleSim.App.ViewModels;
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
        Assert.Equal("Round 1 begins.", result.Events[0].Description);
        Assert.Equal($"{state.GetUnit(state.Plan.UnitOrder[0]).Name} attacks.", result.Events[1].Description);
        Assert.Contains(result.Events, battleEvent =>
            battleEvent.Description == $"{state.GetUnit(state.Plan.UnitOrder[1]).Name} attacks.");

        var plannedAttackers = state.Plan.UnitOrder
            .SelectMany(side => state.Plan.TroopOrders[side])
            .ToArray();
        var actualAttackers = result.Events
            .Where(battleEvent => battleEvent.ActorName is not null)
            .Select(battleEvent => battleEvent.ActorName!)
            .ToArray();

        Assert.Equal(actualAttackers, plannedAttackers.Where(actualAttackers.Contains));
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
    public void BuiltInActions_ScaleFromExpectedStats()
    {
        Assert.Equal(CombatStat.Strength, BuiltInBattleActions.Slash.Scaling.Stat);
        Assert.Equal(CombatStat.Dexterity, BuiltInBattleActions.BowShot.Scaling.Stat);
        Assert.Equal(CombatStat.Strength, BuiltInBattleActions.StaffBonk.Scaling.Stat);
        Assert.Equal(CombatStat.Wisdom, BuiltInBattleActions.Firebolt.Scaling.Stat);
        Assert.Equal(CombatStat.Faith, BuiltInBattleActions.Heal.Scaling.Stat);
    }

    [Fact]
    public void ActionStatScaling_RollsWithinConfiguredStatBasedRange()
    {
        var scaling = new ActionStatScaling(CombatStat.Wisdom, 0.10m, 0.20m);

        var multiplier = scaling.RollMultiplier(statValue: 5, random: new Random(1));

        Assert.InRange(multiplier, 1.50m, 2.00m);
    }

    [Fact]
    public void BuiltInActions_ExposeAccuracyForFutureHitRolls()
    {
        Assert.InRange(BuiltInBattleActions.Slash.Accuracy, 0m, 1m);
        Assert.InRange(BuiltInBattleActions.BowShot.Accuracy, 0m, 1m);
        Assert.InRange(BuiltInBattleActions.Firebolt.Accuracy, 0m, 1m);
        Assert.Equal(1.0m, BuiltInBattleActions.Heal.Accuracy);
    }

    [Fact]
    public void CombatRollRules_CalculateLuckRerollChance()
    {
        Assert.Equal(0.02m, CombatRollRules.GetLuckRerollChance(0));
        Assert.Equal(0.0850m, CombatRollRules.GetLuckRerollChance(10));
        Assert.Equal(0.1500m, CombatRollRules.GetLuckRerollChance(20));
        Assert.Equal(0.2150m, CombatRollRules.GetLuckRerollChance(30));
        Assert.Equal(0.3450m, CombatRollRules.GetLuckRerollChance(50));
        Assert.Equal(0.50m, CombatRollRules.GetLuckRerollChance(100));
    }

    [Fact]
    public void CombatRollRules_CalculateCriticalChance()
    {
        Assert.Equal(0.03m, CombatRollRules.GetCriticalChance(0));
        Assert.Equal(0.070m, CombatRollRules.GetCriticalChance(10));
        Assert.Equal(0.110m, CombatRollRules.GetCriticalChance(20));
        Assert.Equal(0.150m, CombatRollRules.GetCriticalChance(30));
        Assert.Equal(0.230m, CombatRollRules.GetCriticalChance(50));
        Assert.Equal(0.40m, CombatRollRules.GetCriticalChance(100));
    }

    [Fact]
    public void BuiltInActions_DeclareWhetherTheyCanCrit()
    {
        Assert.True(BuiltInBattleActions.Slash.CanCrit);
        Assert.True(BuiltInBattleActions.StaffBonk.CanCrit);
        Assert.True(BuiltInBattleActions.BowShot.CanCrit);
        Assert.False(BuiltInBattleActions.Firebolt.CanCrit);
        Assert.False(BuiltInBattleActions.Heal.CanCrit);
    }

    [Fact]
    public void RunNextAttack_MissedDamageActionLogsMissAndDoesNoDamage()
    {
        var engine = new BattleEngine(new Random(1));
        var missAction = BuiltInBattleActions.Slash with { Accuracy = 0m };
        var attackerDefinition = BuiltInTroopClasses.Fighter with
        {
            ActionProfile = new RowActionProfile(
                Front: new[] { missAction },
                Middle: new[] { missAction },
                Back: new[] { missAction })
        };
        var attacker = new Troop("Attacker", attackerDefinition, new GridPosition(1, 2));
        var target = new Troop("Target", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        attacker.SetBattleAttackLimit(1);
        target.SetBattleAttackLimit(1);
        var state = new BattleState(
            new Unit("Attackers", new[] { attacker }),
            new Unit("Targets", new[] { target }),
            new BattlePlan(
                new[] { BattleSide.Left, BattleSide.Right },
                new Dictionary<BattleSide, IReadOnlyList<string>>
                {
                    [BattleSide.Left] = new[] { attacker.Name },
                    [BattleSide.Right] = new[] { target.Name }
                }));

        var result = engine.RunNextAttack(state);
        var attackEvent = result.Events.Last(battleEvent => battleEvent.ActorName is not null);
        var resultTarget = result.State.RightUnit.Troops.Single();

        Assert.Contains("misses", attackEvent.Description);
        Assert.Equal(0, attackEvent.Damage);
        Assert.Equal(target.Stats.MaxHitPoints, resultTarget.CurrentHitPoints);
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
    public void RangedTargetingRule_CanFollowLeaderPreference()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Archer, new GridPosition(1, 2));
        var frontEnemy = new Troop("Enemy Front", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        var leader = new Troop("Enemy Leader", BuiltInTroopClasses.Wizard, new GridPosition(2, 2));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }, targetingPreference: TargetingPreference.Leader),
            new Unit("Enemies", new[] { frontEnemy, leader }, leaderName: leader.Name),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.BowShot.TargetingRule.SelectTargets(context, BuiltInBattleActions.BowShot);

        Assert.True(selection.HasTargets);
        Assert.Equal("Enemy Leader", selection.Targets.Single().Name);
    }

    [Fact]
    public void RangedTargetingRule_NormalPreferenceTargetsHighestProportionalHealth()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Archer, new GridPosition(1, 2));
        var healthyFarEnemy = new Troop("Healthy Far Enemy", BuiltInTroopClasses.Fighter, new GridPosition(2, 2));
        var damagedNearEnemy = new Troop("Damaged Near Enemy", BuiltInTroopClasses.Wizard, new GridPosition(1, 0));
        damagedNearEnemy.TakeDamage(8);
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }),
            new Unit("Enemies", new[] { damagedNearEnemy, healthyFarEnemy }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.BowShot.TargetingRule.SelectTargets(context, BuiltInBattleActions.BowShot);

        Assert.True(selection.HasTargets);
        Assert.Equal("Healthy Far Enemy", selection.Targets.Single().Name);
    }

    [Fact]
    public void RangedTargetingRule_NormalPreferenceBreaksProportionalHealthTiesByDistance()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Archer, new GridPosition(0, 2));
        var nearEnemy = new Troop("Near Enemy", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var farEnemy = new Troop("Far Enemy", BuiltInTroopClasses.Fighter, new GridPosition(2, 2));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }),
            new Unit("Enemies", new[] { farEnemy, nearEnemy }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.BowShot.TargetingRule.SelectTargets(context, BuiltInBattleActions.BowShot);

        Assert.True(selection.HasTargets);
        Assert.Equal("Near Enemy", selection.Targets.Single().Name);
    }

    [Fact]
    public void RangedTargetingRule_WeakestPreferenceBreaksHitPointTiesByHighestProportionalHealth()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Archer, new GridPosition(1, 2));
        var fighter = new Troop("Enemy Fighter", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        var wizard = new Troop("Enemy Wizard", BuiltInTroopClasses.Wizard, new GridPosition(2, 2));
        fighter.TakeDamage(38);
        wizard.TakeDamage(22);
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }, targetingPreference: TargetingPreference.Weakest),
            new Unit("Enemies", new[] { fighter, wizard }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.BowShot.TargetingRule.SelectTargets(context, BuiltInBattleActions.BowShot);

        Assert.Equal(10, fighter.CurrentHitPoints);
        Assert.Equal(10, wizard.CurrentHitPoints);
        Assert.True(selection.HasTargets);
        Assert.Equal("Enemy Wizard", selection.Targets.Single().Name);
    }

    [Fact]
    public void MeleeTargetingRule_FallsBackWhenPreferredLeaderIsBlocked()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(1, 2));
        var blocker = new Troop("Enemy Blocker", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        var leader = new Troop("Enemy Leader", BuiltInTroopClasses.Wizard, new GridPosition(1, 1));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }, targetingPreference: TargetingPreference.Leader),
            new Unit("Enemies", new[] { blocker, leader }, leaderName: leader.Name),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.True(selection.HasTargets);
        Assert.Equal("Enemy Blocker", selection.Targets.Single().Name);
    }

    [Fact]
    public void MeleeTargetingRule_AttacksBlockerInsteadOfDefaultTargetWhenPreferredTargetIsBlocked()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(1, 2));
        var defaultTarget = new Troop("Default Target", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        var blocker = new Troop("Leader Blocker", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var leader = new Troop("Enemy Leader", BuiltInTroopClasses.Wizard, new GridPosition(0, 1));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }, targetingPreference: TargetingPreference.Leader),
            new Unit("Enemies", new[] { defaultTarget, blocker, leader }, leaderName: leader.Name),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.True(selection.HasTargets);
        Assert.Equal("Leader Blocker", selection.Targets.Single().Name);
    }

    [Fact]
    public void MeleeTargetingRule_UsesDefaultTargetingWhenPreferredTargetIsOutsideReachableLanes()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(2, 2));
        var defaultTarget = new Troop("Default Target", BuiltInTroopClasses.Fighter, new GridPosition(2, 0));
        var blocker = new Troop("Leader Blocker", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var leader = new Troop("Enemy Leader", BuiltInTroopClasses.Wizard, new GridPosition(0, 1));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }, targetingPreference: TargetingPreference.Leader),
            new Unit("Enemies", new[] { defaultTarget, blocker, leader }, leaderName: leader.Name),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.True(selection.HasTargets);
        Assert.Equal("Default Target", selection.Targets.Single().Name);
    }

    [Fact]
    public void MeleeTargetingRule_CanAlwaysTargetTheOnlyLivingEnemy()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(2, 2));
        var onlyEnemy = new Troop("Only Enemy", BuiltInTroopClasses.Wizard, new GridPosition(0, 2));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }),
            new Unit("Enemies", new[] { onlyEnemy }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.True(selection.HasTargets);
        Assert.Equal("Only Enemy", selection.Targets.Single().Name);
    }

    [Fact]
    public void MeleeTargetingRule_PrefersDirectLaneBeforeAdjacentLanes()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(1, 2));
        var direct = new Troop("Enemy Direct", BuiltInTroopClasses.Fighter, new GridPosition(1, 0));
        var adjacent = new Troop("Enemy Adjacent", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }),
            new Unit("Enemies", new[] { adjacent, direct }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.True(selection.HasTargets);
        Assert.Equal("Enemy Direct", selection.Targets.Single().Name);
    }

    [Fact]
    public void MeleeTargetingRule_AdjacentLaneTieUsesDistance()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Fighter, new GridPosition(1, 2));
        var nearAdjacent = new Troop("Near Adjacent", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var farAdjacent = new Troop("Far Adjacent", BuiltInTroopClasses.Fighter, new GridPosition(2, 1));
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }),
            new Unit("Enemies", new[] { farAdjacent, nearAdjacent }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Slash.TargetingRule.SelectTargets(context, BuiltInBattleActions.Slash);

        Assert.True(selection.HasTargets);
        Assert.Equal("Near Adjacent", selection.Targets.Single().Name);
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
            new Unit("Enemies", Array.Empty<Troop>()),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Heal.TargetingRule.SelectTargets(context, BuiltInBattleActions.Heal);

        Assert.True(selection.HasTargets);
        Assert.Equal("Heavily Damaged", selection.Targets.Single().Name);
    }

    [Fact]
    public void MostDamagedAllyTargetingRule_FallsBackToLowestMaxHitPointsWhenNoAllyIsDamaged()
    {
        var cleric = new Troop("Cleric", BuiltInTroopClasses.Cleric, new GridPosition(1, 1));
        var fighter = new Troop("Fighter", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var archer = new Troop("Archer", BuiltInTroopClasses.Archer, new GridPosition(2, 0));
        var context = new TargetingContext(
            cleric,
            new Unit("Allies", new[] { fighter, cleric, archer }),
            new Unit("Enemies", Array.Empty<Troop>()),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = BuiltInBattleActions.Heal.TargetingRule.SelectTargets(context, BuiltInBattleActions.Heal);

        Assert.True(selection.HasTargets);
        Assert.Equal("Cleric", selection.Targets.Single().Name);
    }

    [Fact]
    public void EntireUnitTargetingRule_TargetsAllLivingEnemiesAndIgnoresPreference()
    {
        var attacker = new Troop("Attacker", BuiltInTroopClasses.Wizard, new GridPosition(1, 1));
        var enemyOne = new Troop("Enemy One", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var enemyTwo = new Troop("Enemy Two", BuiltInTroopClasses.Archer, new GridPosition(1, 0));
        var defeatedEnemy = new Troop("Defeated Enemy", BuiltInTroopClasses.Cleric, new GridPosition(2, 0));
        defeatedEnemy.TakeDamage(defeatedEnemy.Stats.MaxHitPoints);
        var action = BuiltInBattleActions.Firebolt with
        {
            TargetingRule = new EntireUnitTargetingRule(),
            TargetSide = TargetSide.Enemy
        };
        var context = new TargetingContext(
            attacker,
            new Unit("Allies", new[] { attacker }, targetingPreference: TargetingPreference.Leader),
            new Unit("Enemies", new[] { enemyOne, enemyTwo, defeatedEnemy }, leaderName: defeatedEnemy.Name),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = action.TargetingRule.SelectTargets(context, action);

        Assert.Equal(new[] { "Enemy One", "Enemy Two" }, selection.Targets.Select(troop => troop.Name));
    }

    [Fact]
    public void EntireUnitTargetingRule_TargetsAllLivingAlliesAndIgnoresPreference()
    {
        var cleric = new Troop("Cleric", BuiltInTroopClasses.Cleric, new GridPosition(1, 1));
        var fighter = new Troop("Fighter", BuiltInTroopClasses.Fighter, new GridPosition(0, 0));
        var defeatedAlly = new Troop("Defeated Ally", BuiltInTroopClasses.Archer, new GridPosition(2, 0));
        defeatedAlly.TakeDamage(defeatedAlly.Stats.MaxHitPoints);
        var action = BuiltInBattleActions.Heal with
        {
            TargetingRule = new EntireUnitTargetingRule(),
            TargetSide = TargetSide.Ally
        };
        var context = new TargetingContext(
            cleric,
            new Unit("Allies", new[] { cleric, fighter, defeatedAlly }, targetingPreference: TargetingPreference.Weakest),
            new Unit("Enemies", Array.Empty<Troop>()),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

        var selection = action.TargetingRule.SelectTargets(context, action);

        Assert.Equal(new[] { "Cleric", "Fighter" }, selection.Targets.Select(troop => troop.Name));
    }

    [Fact]
    public void BuiltInClassHitPoints_UseDoubledPools()
    {
        Assert.Equal(48, BuiltInTroopClasses.Fighter.BaseStats.MaxHitPoints);
        Assert.Equal(40, BuiltInTroopClasses.Archer.BaseStats.MaxHitPoints);
        Assert.Equal(36, BuiltInTroopClasses.Cleric.BaseStats.MaxHitPoints);
        Assert.Equal(32, BuiltInTroopClasses.Wizard.BaseStats.MaxHitPoints);
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
            new Unit("Enemies", new[] { defeatedEnemy }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));

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
    public void HealingAction_ProducesHelpfulBattleEventIntent()
    {
        var engine = new BattleEngine(new Random(1));
        var cleric = new Troop("Blue Cleric", BuiltInTroopClasses.Cleric, new GridPosition(1, 1));
        var fighter = new Troop("Blue Fighter", BuiltInTroopClasses.Fighter, new GridPosition(1, 2));
        cleric.SetBattleAttackLimit(2);
        fighter.SetBattleAttackLimit(1);
        fighter.TakeDamage(8);
        var state = new BattleState(
            new Unit("Blue Unit", new[] { cleric, fighter }),
            new Unit("Red Unit", Array.Empty<Troop>()),
            new BattlePlan(
                new[] { BattleSide.Left },
                new Dictionary<BattleSide, IReadOnlyList<string>>
                {
                    [BattleSide.Left] = new[] { cleric.Name },
                    [BattleSide.Right] = Array.Empty<string>()
                }));

        var result = engine.RunNextAttack(state);
        var healEvent = result.Events.Single(battleEvent => battleEvent.Description.Contains(" uses Heal "));

        Assert.Equal(BattleEventIntent.Helpful, healEvent.Intent);
        Assert.Equal("Blue Fighter", healEvent.TargetName);
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

    [Fact]
    public void MoveTroop_BeforeBattleStartMovesToEmptyOwnSlotAndRecalculatesActions()
    {
        var state = BattleState.CreateDefault(seed: 1);

        var movedState = state.MoveTroop(BattleSide.Left, "Blue Fighter", new GridPosition(1, 2));
        var movedFighter = movedState.LeftUnit.Troops.Single(troop => troop.Name == "Blue Fighter");

        Assert.Equal(new GridPosition(1, 2), movedFighter.Position);
        Assert.Equal(3, movedFighter.MaxBattleAttacks);
        Assert.Equal(3, movedFighter.RemainingBattleAttacks);
    }

    [Fact]
    public void CreateDefault_AssignsUnitLeaders()
    {
        var state = BattleState.CreateDefault(seed: 1);

        Assert.Equal("Blue Cleric", state.LeftUnit.LeaderName);
        Assert.Equal("Red Fighter", state.RightUnit.LeaderName);
    }

    [Fact]
    public void UnitLeaders_ArePreservedWhenFormationChanges()
    {
        var state = BattleState.CreateDefault(seed: 1);

        var rotatedState = state.RotateFormationClockwise(BattleSide.Left);
        var movedState = rotatedState.MoveTroop(BattleSide.Left, "Blue Cleric", new GridPosition(0, 0));

        Assert.Equal("Blue Cleric", movedState.LeftUnit.LeaderName);
        Assert.True(movedState.LeftUnit.IsLeader(movedState.LeftUnit.Troops.Single(troop => troop.Name == "Blue Cleric")));
    }

    [Fact]
    public void MoveTroop_BeforeBattleStartRequiresEmptySlot()
    {
        var state = BattleState.CreateDefault(seed: 1);

        Assert.Throws<InvalidOperationException>(() =>
            state.MoveTroop(BattleSide.Left, "Blue Fighter", new GridPosition(0, 1)));
    }

    [Fact]
    public void MoveTroop_ToCurrentSlotIsAllowedAsNoOp()
    {
        var state = BattleState.CreateDefault(seed: 1);

        var unchangedState = state.MoveTroop(BattleSide.Left, "Blue Fighter", new GridPosition(1, 0));

        Assert.Same(state, unchangedState);
    }

    [Fact]
    public void MoveTroop_IsBlockedAfterBattleStarts()
    {
        var engine = new BattleEngine();
        var state = engine.RunNextAttack(BattleState.CreateDefault(seed: 1)).State;

        Assert.Throws<InvalidOperationException>(() =>
            state.MoveTroop(BattleSide.Left, "Blue Fighter", new GridPosition(1, 2)));
    }

    [Fact]
    public void UnitTemplateRepository_LoadsValidTemplateFromJson()
    {
        var path = FindRepoFile("src/BattleSim.App/Data/UnitTemplates/blue-balanced-test.json");
        var repository = new UnitTemplateRepository(Path.GetDirectoryName(path)!, BuiltInTroopClasses.ById);

        var templates = repository.LoadAll();
        var template = templates.Single(candidate => candidate.Id == "blue-balanced-test");

        Assert.Equal("Blue Unit", template.Name);
        Assert.Equal(3, template.Troops.Count);
        Assert.Contains(template.Troops, troop => troop.Name == "Blue Cleric" && troop.IsLeader);
    }

    [Fact]
    public void UnitTemplateValidation_RejectsZeroTroops()
    {
        var template = CreateTemplate(Array.Empty<UnitTemplateTroop>());

        Assert.Throws<UnitTemplateValidationException>(() =>
            UnitTemplateValidator.ValidateAndThrow(template, BuiltInTroopClasses.ById));
    }

    [Fact]
    public void UnitTemplateValidation_RejectsMoreThanFiveTroops()
    {
        var troops = Enumerable.Range(0, 6)
            .Select(index => CreateTemplateTroop($"fighter-{index}", row: index / 3, column: index % 3))
            .ToArray();
        var template = CreateTemplate(troops);

        Assert.Throws<UnitTemplateValidationException>(() =>
            UnitTemplateValidator.ValidateAndThrow(template, BuiltInTroopClasses.ById));
    }

    [Fact]
    public void UnitTemplateValidation_RejectsDuplicateGridPositions()
    {
        var template = CreateTemplate(
        [
            CreateTemplateTroop("fighter-1", row: 1, column: 1),
            CreateTemplateTroop("fighter-2", row: 1, column: 1)
        ]);

        Assert.Throws<UnitTemplateValidationException>(() =>
            UnitTemplateValidator.ValidateAndThrow(template, BuiltInTroopClasses.ById));
    }

    [Fact]
    public void UnitTemplateValidation_RejectsOutOfBoundsPositions()
    {
        var template = CreateTemplate([CreateTemplateTroop("fighter-1", row: 3, column: 1)]);

        Assert.Throws<UnitTemplateValidationException>(() =>
            UnitTemplateValidator.ValidateAndThrow(template, BuiltInTroopClasses.ById));
    }

    [Fact]
    public void UnitTemplateValidation_RejectsUnknownTroopClassId()
    {
        var template = CreateTemplate(
        [
            CreateTemplateTroop("mystery-1", row: 1, column: 1, troopClassId: "mystery")
        ]);

        Assert.Throws<UnitTemplateValidationException>(() =>
            UnitTemplateValidator.ValidateAndThrow(template, BuiltInTroopClasses.ById));
    }

    [Fact]
    public void UnitTemplateValidation_RejectsMoreThanOneLeader()
    {
        var template = CreateTemplate(
        [
            CreateTemplateTroop("fighter-1", row: 0, column: 0, isLeader: true),
            CreateTemplateTroop("fighter-2", row: 1, column: 0, isLeader: true)
        ]);

        Assert.Throws<UnitTemplateValidationException>(() =>
            UnitTemplateValidator.ValidateAndThrow(template, BuiltInTroopClasses.ById));
    }

    [Fact]
    public void UnitFactory_CreatesRuntimeTroopsFromClassDefinitions()
    {
        var template = CreateTemplate(
        [
            CreateTemplateTroop("archer-1", "Template Archer", "archer", row: 1, column: 0, isLeader: true)
        ]);
        var factory = new UnitFactory(BuiltInTroopClasses.ById);

        var unit = factory.Create(template, BattleSide.Left);
        var troop = unit.Troops.Single();

        Assert.Equal("Template Archer", troop.Name);
        Assert.Equal(BuiltInTroopClasses.Archer.Id, troop.ClassDefinition.Id);
        Assert.Equal(BuiltInTroopClasses.Archer.BaseStats, troop.Stats);
        Assert.Equal(BuiltInTroopClasses.Archer.PortraitAssetPath, troop.ClassDefinition.PortraitAssetPath);
        Assert.Equal(new GridPosition(1, 2), troop.Position);
        Assert.Equal("Template Archer", unit.LeaderName);
    }

    [Fact]
    public void UnitFactory_MirrorsLocalFormationForOpposingSides()
    {
        var frontLeftTroop = CreateTemplateTroop("fighter-1", row: 0, column: 0);

        Assert.Equal(new GridPosition(0, 2), UnitFactory.ToRuntimePosition(frontLeftTroop, BattleSide.Left));
        Assert.Equal(new GridPosition(2, 0), UnitFactory.ToRuntimePosition(frontLeftTroop, BattleSide.Right));
    }

    [Fact]
    public void BattleState_CreateFromTemplates_AllowsSideSwappingAndMirrorMatches()
    {
        var blueTemplate = BattleState.CreateDefaultLeftTemplate();
        var state = BattleState.CreateFromTemplates(blueTemplate, blueTemplate, seed: 1);

        Assert.Equal("Blue Unit", state.LeftUnit.Name);
        Assert.Equal("Blue Unit", state.RightUnit.Name);
        Assert.Contains(state.LeftUnit.Troops, troop => troop.Name == "Blue Fighter" && troop.Position == new GridPosition(1, 0));
        Assert.Contains(state.RightUnit.Troops, troop => troop.Name == "Blue Fighter" && troop.Position == new GridPosition(1, 2));
    }

    [Fact]
    public void ReplaceUnitFromTemplate_PreservesOpposingPreBattleFormation()
    {
        var state = BattleState.CreateDefault(seed: 1)
            .RotateFormationClockwise(BattleSide.Right);
        var rightPositionsBeforeReplacement = state.RightUnit.Troops
            .ToDictionary(troop => troop.Name, troop => troop.Position);

        var replacementTemplate = CreateTemplate(
        [
            CreateTemplateTroop("fighter-leader", "Replacement Fighter", "fighter", row: 1, column: 0, isLeader: true)
        ]);
        var replacedState = state.ReplaceUnitFromTemplate(BattleSide.Left, replacementTemplate, seed: 1);

        Assert.Equal(rightPositionsBeforeReplacement, replacedState.RightUnit.Troops.ToDictionary(troop => troop.Name, troop => troop.Position));
        Assert.Single(replacedState.LeftUnit.Troops);
        Assert.Equal("Replacement Fighter", replacedState.LeftUnit.Troops.Single().Name);
    }

    [Fact]
    public void ReplaceUnitFromTemplate_IsBlockedAfterBattleStarts()
    {
        var engine = new BattleEngine();
        var state = engine.RunNextAttack(BattleState.CreateDefault(seed: 1)).State;

        Assert.Throws<InvalidOperationException>(() =>
            state.ReplaceUnitFromTemplate(BattleSide.Left, BattleState.CreateDefaultLeftTemplate()));
    }

    [Fact]
    public void UnitTemplate_DoesNotStoreRuntimeBattleState()
    {
        var templateTroopProperties = typeof(UnitTemplateTroop)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("CurrentHitPoints", templateTroopProperties);
        Assert.DoesNotContain("RemainingBattleAttacks", templateTroopProperties);
        Assert.DoesNotContain("MaxBattleAttacks", templateTroopProperties);
    }

    [Fact]
    public void FormationBuilder_NewDraftStartsEmptyAndInvalid()
    {
        var builder = CreateFormationBuilder();

        Assert.Empty(builder.DraftTroops);
        Assert.False(builder.CanSave);
        Assert.NotEmpty(builder.ValidationMessages);
    }

    [Fact]
    public void FormationBuilder_AddTroopUsesKnownClassAndFirstEmptyGridPosition()
    {
        var builder = CreateFormationBuilder();
        builder.DraftTemplateName = "Builder Test";

        builder.AddTroop();

        var troop = builder.DraftTroops.Single();
        Assert.Equal("archer", troop.TroopClassId);
        Assert.Equal(0, troop.Row);
        Assert.Equal(0, troop.Column);
        Assert.True(troop.IsLeader);
    }

    [Fact]
    public void FormationBuilder_CannotAddMoreThanFiveTroops()
    {
        var builder = CreateFormationBuilder();

        for (var index = 0; index < 6; index++)
        {
            builder.AddTroop();
        }

        Assert.Equal(5, builder.DraftTroops.Count);
        Assert.False(builder.CanAddTroop);
    }

    [Fact]
    public void FormationBuilder_CannotMoveTroopOutOfBounds()
    {
        var builder = CreateFormationBuilder();
        builder.AddTroop();

        var moved = builder.MoveSelectedTroop(new GridPosition(4, 1));

        Assert.False(moved);
        Assert.Equal(0, builder.SelectedDraftTroop!.Row);
        Assert.Equal(0, builder.SelectedDraftTroop.Column);
    }

    [Fact]
    public void FormationBuilder_CannotMoveTroopIntoOccupiedCell()
    {
        var builder = CreateFormationBuilder();
        builder.AddTroop();
        var firstTroop = builder.SelectedDraftTroop!;
        builder.AddTroop();
        var secondTroop = builder.SelectedDraftTroop!;

        var moved = builder.MoveSelectedTroop(new GridPosition(firstTroop.Row, firstTroop.Column));

        Assert.False(moved);
        Assert.Equal(0, firstTroop.Row);
        Assert.Equal(0, firstTroop.Column);
        Assert.Equal(0, secondTroop.Row);
        Assert.Equal(1, secondTroop.Column);
    }

    [Fact]
    public void FormationBuilder_RemovingTroopFreesGridCell()
    {
        var builder = CreateFormationBuilder();
        builder.AddTroop();
        builder.RemoveSelectedTroop();
        builder.AddTroop();

        var troop = builder.DraftTroops.Single();
        Assert.Equal(0, troop.Row);
        Assert.Equal(0, troop.Column);
    }

    [Fact]
    public void FormationBuilder_SaveProducesJsonLoadableByRepository()
    {
        var templateDirectory = CreateTempTemplateDirectory();
        var builder = CreateFormationBuilder(templateDirectory);
        builder.DraftTemplateName = "Saved Builder Unit";
        builder.AddTroop();

        builder.SaveTemplate();

        var repository = new UnitTemplateRepository(templateDirectory, BuiltInTroopClasses.ById);
        var template = repository.LoadAll().Single();
        Assert.Equal("saved-builder-unit", template.Id);
        Assert.Equal("Saved Builder Unit", template.Name);
        Assert.Single(template.Troops);
    }

    [Fact]
    public void FormationBuilder_ApplyDraftUsesExistingBattleTemplateFlow()
    {
        BattleState? state = null;
        var builder = new FormationBuilderViewModel(
            CreateTempTemplateDirectory(),
            BuiltInTroopClasses.ById,
            (side, template) =>
            {
                state = BattleState.CreateDefault(seed: 1).ReplaceUnitFromTemplate(side, template, seed: 1);
            });
        builder.DraftTemplateName = "Applied Builder Unit";
        builder.AddTroop();

        builder.ApplyToBlue();

        Assert.NotNull(state);
        Assert.Equal("Applied Builder Unit", state!.LeftUnit.Name);
        Assert.Single(state.LeftUnit.Troops);
        Assert.Equal(BuiltInTroopClasses.Archer.Id, state.LeftUnit.Troops.Single().ClassDefinition.Id);
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

    private static UnitTemplate CreateTemplate(IReadOnlyList<UnitTemplateTroop> troops)
    {
        return new UnitTemplate
        {
            Id = "test-unit",
            Name = "Test Unit",
            Troops = troops
        };
    }

    private static UnitTemplateTroop CreateTemplateTroop(
        string slotId,
        string name = "Test Fighter",
        string troopClassId = "fighter",
        int row = 1,
        int column = 1,
        bool isLeader = false)
    {
        return new UnitTemplateTroop
        {
            SlotId = slotId,
            Name = name,
            TroopClassId = troopClassId,
            Row = row,
            Column = column,
            IsLeader = isLeader
        };
    }

    private static FormationBuilderViewModel CreateFormationBuilder(string? templateDirectory = null)
    {
        return new FormationBuilderViewModel(
            templateDirectory ?? CreateTempTemplateDirectory(),
            BuiltInTroopClasses.ById);
    }

    private static string CreateTempTemplateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "BattleSimTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
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
            new Unit("Enemies", new[] { backEnemy, frontEnemy }),
            FormationOrientation.FrontOnRight,
            FormationOrientation.FrontOnLeft,
            new Random(1));
    }
}

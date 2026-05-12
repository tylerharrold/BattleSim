using BattleSim.Engine;
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

        Assert.Equal(3, setupEvents.Count);
        Assert.StartsWith("Unit order:", setupEvents[0].Description);
        Assert.StartsWith("Blue Unit troop order:", setupEvents[1].Description);
        Assert.StartsWith("Red Unit troop order:", setupEvents[2].Description);
    }
}

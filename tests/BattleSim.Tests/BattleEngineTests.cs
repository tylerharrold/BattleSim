using BattleSim.Engine;
using Xunit;

namespace BattleSim.Tests;

public sealed class BattleEngineTests
{
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
            .Select(battleEvent => battleEvent.ActorName)
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

using BattleSim.Engine;
using Xunit;

namespace BattleSim.Tests;

public sealed class BattleEngineTests
{
    [Fact]
    public void RunOneTurn_ProducesDeterministicEventsAndAdvancesTurn()
    {
        var engine = new BattleEngine();
        var state = BattleState.CreateDefault();

        var result = engine.RunOneTurn(state);

        Assert.Equal(2, result.State.TurnNumber);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("Blue Archer hits Red Wizard for 4 damage.", result.Events[0].Description);
        Assert.Equal("Red Archer hits Blue Archer for 4 damage.", result.Events[1].Description);
    }
}

namespace BattleSim.Engine;

public interface IBattleActionResolver
{
    BattleActionResult Resolve(BattleActionResolutionRequest request);
}

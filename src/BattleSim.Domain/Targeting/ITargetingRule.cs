using BattleSim.Domain.Models;

namespace BattleSim.Domain.Targeting;

public interface ITargetingRule
{
    TargetSelection SelectTargets(TargetingContext context, BattleActionDefinition action);
}

using BattleSim.Domain.Models;
using BattleSim.Engine;

namespace BattleSim.App.ViewModels;

public sealed record BattleLogEntryViewModel(
    string Description,
    BattleSide? ActorSide = null,
    GridPosition? ActorPosition = null,
    BattleSide? TargetSide = null,
    GridPosition? TargetPosition = null,
    BattleEventIntent Intent = BattleEventIntent.Neutral)
{
    public bool IsAttack => ActorSide.HasValue && ActorPosition.HasValue && TargetSide.HasValue && TargetPosition.HasValue;

    public override string ToString() => Description;
}

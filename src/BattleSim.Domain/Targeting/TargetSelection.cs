using BattleSim.Domain.Models;

namespace BattleSim.Domain.Targeting;

public sealed class TargetSelection
{
    private TargetSelection(IEnumerable<Troop> targets)
    {
        Targets = targets.ToArray();
    }

    public IReadOnlyList<Troop> Targets { get; }

    public bool HasTargets => Targets.Count > 0;

    public static TargetSelection Empty { get; } = new(Array.Empty<Troop>());

    public static TargetSelection Single(Troop? target)
    {
        return target is null ? Empty : new TargetSelection(new[] { target });
    }

    public static TargetSelection From(IEnumerable<Troop> targets)
    {
        var livingTargets = targets.Where(troop => !troop.IsDefeated).ToArray();
        return livingTargets.Length == 0 ? Empty : new TargetSelection(livingTargets);
    }
}

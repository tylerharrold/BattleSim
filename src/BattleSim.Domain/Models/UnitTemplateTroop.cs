namespace BattleSim.Domain.Models;

public sealed class UnitTemplateTroop
{
    public string SlotId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string TroopClassId { get; init; } = string.Empty;

    public int Row { get; init; }

    public int Column { get; init; }

    public bool IsLeader { get; init; }
}

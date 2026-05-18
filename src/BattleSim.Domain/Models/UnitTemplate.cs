namespace BattleSim.Domain.Models;

public sealed class UnitTemplate
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    // Template positions are local to the unit, not screen coordinates.
    // Column 0 is the unit's front row; Row 0 is the unit's left side.
    public IReadOnlyList<UnitTemplateTroop> Troops { get; init; } = Array.Empty<UnitTemplateTroop>();
}

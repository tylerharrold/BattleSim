namespace BattleSim.Domain.Models;

public sealed class UnitTemplate
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    // Template positions are local to the unit, not screen coordinates.
    // Row 0 is the unit's front/north row in the formation builder; Column 0 is the unit's left side.
    public IReadOnlyList<UnitTemplateTroop> Troops { get; init; } = Array.Empty<UnitTemplateTroop>();
}

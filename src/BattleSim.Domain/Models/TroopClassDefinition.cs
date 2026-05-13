namespace BattleSim.Domain.Models;

public sealed record TroopClassDefinition(
    string Id,
    string DisplayName,
    Stats BaseStats,
    RowActionProfile ActionProfile);

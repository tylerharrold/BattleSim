namespace BattleSim.Domain.Models;

public sealed record TroopClassDefinition(
    string Id,
    string DisplayName,
    Stats BaseStats,
    RowActionProfile ActionProfile,
    // Prototype presentation metadata. This stays as a plain asset path string so Domain still has no UI framework dependency.
    string PortraitAssetPath);

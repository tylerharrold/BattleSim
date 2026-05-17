using Avalonia.Media;
using BattleSim.Domain.Models;
using BattleSim.Engine;

namespace BattleSim.App.ViewModels;

public sealed record TroopDetailViewModel(
    string Name,
    string ClassName,
    IImage? PortraitImage,
    string SideLabel,
    string PositionLabel,
    string FormationRankLabel,
    string HitPointsLabel,
    string ActionCountLabel,
    string NextActionLabel,
    IReadOnlyList<TroopDetailActionViewModel> CurrentRowActions,
    Stats Stats);

public sealed record TroopDetailActionViewModel(
    int Number,
    string Name,
    string StatusLabel,
    IBrush TextBrush);

using Avalonia;
using Avalonia.Media;

namespace BattleSim.App.ViewModels;

public sealed record GridCellViewModel(
    string Name,
    string ClassName,
    string PortraitAssetPath,
    IImage? PortraitImage,
    bool HasPortrait,
    string HitPointsLabel,
    string AttacksLabel,
    IBrush BorderBrush,
    Thickness BorderThickness)
{
    public static GridCellViewModel Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        false,
        string.Empty,
        string.Empty,
        Brushes.Gray,
        new Thickness(1));
}

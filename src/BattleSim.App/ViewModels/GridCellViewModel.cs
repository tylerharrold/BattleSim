using Avalonia;
using Avalonia.Media;

namespace BattleSim.App.ViewModels;

public sealed record GridCellViewModel(
    string Name,
    string ClassName,
    string HitPointsLabel,
    IBrush BorderBrush,
    Thickness BorderThickness)
{
    public static GridCellViewModel Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        Brushes.Gray,
        new Thickness(1));
}

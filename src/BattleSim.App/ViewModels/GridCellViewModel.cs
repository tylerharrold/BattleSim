namespace BattleSim.App.ViewModels;

public sealed record GridCellViewModel(string Name, string ClassName, string HitPointsLabel)
{
    public static GridCellViewModel Empty { get; } = new(string.Empty, string.Empty, string.Empty);
}

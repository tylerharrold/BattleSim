using Avalonia.Media;
using BattleSim.Domain.Models;

namespace BattleSim.App.ViewModels;

public sealed class FormationBuilderCellViewModel
{
    public FormationBuilderCellViewModel(
        GridPosition position,
        string label,
        string className,
        IImage? portraitImage,
        bool isOccupied,
        bool isSelected,
        bool isLeader,
        bool isDropPreview,
        bool isLegalDropPreview)
    {
        Position = position;
        Label = label;
        ClassName = className;
        PortraitImage = portraitImage;
        IsOccupied = isOccupied;
        IsSelected = isSelected;
        IsLeader = isLeader;
        IsDropPreview = isDropPreview;
        IsLegalDropPreview = isLegalDropPreview;
    }

    public GridPosition Position { get; }

    public string Label { get; }

    public string ClassName { get; }

    public IImage? PortraitImage { get; }

    public bool HasPortrait => PortraitImage is not null;

    public bool IsOccupied { get; }

    public bool IsSelected { get; }

    public bool IsLeader { get; }

    public bool IsDropPreview { get; }

    public bool IsLegalDropPreview { get; }

    public IBrush Background => IsSelected
        ? Brush.Parse("#D9ECFF")
        : IsDropPreview
            ? IsLegalDropPreview ? Brush.Parse("#BFE3FF") : Brush.Parse("#FFD0D0")
        : Brushes.White;

    public IBrush BorderBrush => IsSelected
        ? Brushes.DeepSkyBlue
        : IsOccupied
            ? Brushes.DimGray
            : Brushes.LightGray;
}

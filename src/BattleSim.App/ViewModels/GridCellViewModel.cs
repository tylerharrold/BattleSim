using Avalonia;
using Avalonia.Media;
using BattleSim.Domain.Models;
using BattleSim.Engine;

namespace BattleSim.App.ViewModels;

public sealed record GridCellViewModel(
    BattleSide Side,
    GridPosition Position,
    bool HasTroop,
    string Name,
    string ClassName,
    string PortraitAssetPath,
    IImage? PortraitImage,
    bool HasPortrait,
    bool IsLeader,
    string HitPointsLabel,
    string AttacksLabel,
    IBrush BackgroundBrush,
    IBrush BorderBrush,
    Thickness BorderThickness,
    IBrush PortraitBorderBrush,
    Thickness PortraitBorderThickness);

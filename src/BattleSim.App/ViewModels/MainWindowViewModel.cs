using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BattleSim.Domain.Models;
using BattleSim.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BattleSim.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private const double ArrowHeadLength = 16;
    private const double ArrowHeadWidth = 8;
    private static readonly IBrush LeftCellBackground = Brush.Parse("#F6FAFF");
    private static readonly IBrush RightCellBackground = Brush.Parse("#FFF7F7");
    private static readonly IBrush LegalDropBackground = Brush.Parse("#BFE3FF");
    private static readonly IBrush IllegalDropBackground = Brush.Parse("#FFD0D0");

    // The view model adapts engine state into bindable UI data without owning combat rules.
    private readonly BattleEngine battleEngine = new();
    private readonly Dictionary<string, IImage> portraitCache = new();
    private DraggedTroop? draggedTroop;
    private DropPreview? dropPreview;

    private BattleState battleState = BattleState.CreateDefault();

    public MainWindowViewModel()
    {
        RebuildSetupLog("Battle initialized.");
        RefreshFromState();
    }

    public ObservableCollection<GridCellViewModel> LeftGridCells { get; } = new();

    public ObservableCollection<GridCellViewModel> RightGridCells { get; } = new();

    public ObservableCollection<BattleLogEntryViewModel> BattleLog { get; } = new();

    [ObservableProperty]
    private string leftUnitName = string.Empty;

    [ObservableProperty]
    private string rightUnitName = string.Empty;

    [ObservableProperty]
    private string roundLabel = string.Empty;

    [ObservableProperty]
    private BattleLogEntryViewModel? selectedBattleLogEntry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRotateFormations))]
    private bool battleHasStarted;

    [ObservableProperty]
    private bool isAttackArrowVisible;

    [ObservableProperty]
    private Point attackArrowStart;

    [ObservableProperty]
    private Point attackArrowEnd;

    [ObservableProperty]
    private string attackArrowPathData = string.Empty;

    [ObservableProperty]
    private IBrush attackArrowBrush = Brushes.Red;

    [ObservableProperty]
    private bool isDraggingTroop;

    [ObservableProperty]
    private IImage? draggedPortraitImage;

    [ObservableProperty]
    private double draggedPortraitLeft;

    [ObservableProperty]
    private double draggedPortraitTop;

    public bool CanRotateFormations => !BattleHasStarted;

    public event Action<BattleLogEntryViewModel>? AttackArrowRequested;

    [RelayCommand]
    private void RotateLeftFormation()
    {
        RotateFormation(BattleSide.Left, "Blue formation rotated clockwise.");
    }

    [RelayCommand]
    private void RotateRightFormation()
    {
        RotateFormation(BattleSide.Right, "Red formation rotated clockwise.");
    }

    [RelayCommand]
    private void RunNextAttack()
    {
        BattleHasStarted = true;
        ApplyStepResult(battleEngine.RunNextAttack(battleState), selectLatestAttack: true);
    }

    [RelayCommand]
    private void RunNextTurn()
    {
        BattleHasStarted = true;
        ApplyStepResult(battleEngine.RunNextTurn(battleState), selectLatestAttack: false);
    }

    [RelayCommand]
    private void RunOneRound()
    {
        BattleHasStarted = true;
        ApplyStepResult(battleEngine.RunOneRound(battleState), selectLatestAttack: false);
    }

    partial void OnSelectedBattleLogEntryChanged(BattleLogEntryViewModel? value)
    {
        RefreshFromState();

        if (value?.IsAttack == true)
        {
            AttackArrowRequested?.Invoke(value);
            return;
        }

        HideAttackArrow();
    }

    private void ApplyStepResult(BattleStepResult result, bool selectLatestAttack)
    {
        battleState = result.State;
        BattleLogEntryViewModel? latestAttack = null;

        foreach (var battleEvent in result.Events)
        {
            var entry = ToLogEntry(battleEvent);
            BattleLog.Add(entry);

            if (entry.IsAttack)
            {
                latestAttack = entry;
            }
        }

        if (battleState.IsComplete)
        {
            BattleLog.Add(new BattleLogEntryViewModel("Battle complete."));
        }

        SelectedBattleLogEntry = selectLatestAttack ? latestAttack : null;
        RefreshFromState();
    }

    public void ShowAttackArrow(Point start, Point end, BattleEventIntent intent)
    {
        AttackArrowStart = start;
        AttackArrowEnd = end;
        AttackArrowPathData = CreateArrowPathData(start, end);
        AttackArrowBrush = GetAttackArrowBrush(intent);
        IsAttackArrowVisible = true;
    }

    public void HideAttackArrow()
    {
        IsAttackArrowVisible = false;
        AttackArrowPathData = string.Empty;
    }

    public bool CanBeginTroopDrag(GridCellViewModel cell)
    {
        return CanRotateFormations && cell.HasTroop;
    }

    public bool CanShowTroopDetails(GridCellViewModel cell)
    {
        return BattleHasStarted && cell.HasTroop && !IsDraggingTroop;
    }

    public TroopDetailViewModel? CreateTroopDetail(GridCellViewModel cell)
    {
        if (!CanShowTroopDetails(cell))
        {
            return null;
        }

        var troop = battleState.GetUnit(cell.Side).Troops.FirstOrDefault(candidate => candidate.Position == cell.Position);
        if (troop is null)
        {
            return null;
        }

        var actions = BattleActionRules.GetBattleActions(troop, cell.Side);
        var usedActionCount = troop.MaxBattleAttacks - troop.RemainingBattleAttacks;
        var nextAction = !troop.IsDefeated && troop.RemainingBattleAttacks > 0 && usedActionCount < actions.Count
            ? actions[usedActionCount].DisplayName
            : "None";
        var actionRows = actions
            .Select((action, index) => new TroopDetailActionViewModel(
                index + 1,
                action.DisplayName,
                index < usedActionCount ? "Used" : index == usedActionCount && troop.RemainingBattleAttacks > 0 ? "Next" : "Pending",
                index < usedActionCount ? Brushes.Gray : Brushes.Black))
            .ToArray();

        return new TroopDetailViewModel(
            troop.Name,
            troop.ClassDefinition.DisplayName,
            GetPortraitImage(troop.ClassDefinition.PortraitAssetPath),
            cell.Side == BattleSide.Left ? "Blue Unit" : "Red Unit",
            $"Row {troop.Position.Row + 1}, Column {troop.Position.Column + 1}",
            BattleActionRules.GetFormationRank(troop.Position, cell.Side).ToString(),
            $"{troop.CurrentHitPoints}/{troop.Stats.MaxHitPoints} HP",
            $"{troop.RemainingBattleAttacks}/{troop.MaxBattleAttacks} attacks left",
            nextAction,
            actionRows,
            troop.Stats);
    }

    public void BeginTroopDrag(GridCellViewModel cell, Point pointerPosition)
    {
        if (!CanBeginTroopDrag(cell))
        {
            return;
        }

        SelectedBattleLogEntry = null;
        draggedTroop = new DraggedTroop(cell.Side, cell.Position, cell.Name, cell.PortraitImage);
        IsDraggingTroop = true;
        DraggedPortraitImage = cell.PortraitImage;
        UpdateDraggedPortraitPosition(pointerPosition);
        RefreshFromState();
    }

    public void UpdateTroopDrag(Point pointerPosition, BattleSide? hoverSide, GridPosition? hoverPosition)
    {
        if (!IsDraggingTroop)
        {
            return;
        }

        UpdateDraggedPortraitPosition(pointerPosition);
        dropPreview = hoverSide.HasValue && hoverPosition.HasValue
            ? new DropPreview(hoverSide.Value, hoverPosition.Value, IsLegalDropTarget(hoverSide.Value, hoverPosition.Value))
            : null;
        RefreshFromState();
    }

    public void CompleteTroopDrag(BattleSide? targetSide, GridPosition? targetPosition)
    {
        if (draggedTroop is null)
        {
            ClearTroopDrag();
            return;
        }

        var movedTroopName = draggedTroop.Name;
        var movedSide = draggedTroop.Side;
        var destinationSide = targetSide;
        var destinationPosition = targetPosition;

        // Pointer release can occasionally land just outside the rendered slot after the hover preview
        // was already valid. Keep the last legal preview as a forgiving drop target.
        if ((!destinationSide.HasValue ||
             !destinationPosition.HasValue ||
             !IsLegalDropTarget(destinationSide.Value, destinationPosition.Value)) &&
            dropPreview?.IsLegal == true)
        {
            destinationSide = dropPreview.Side;
            destinationPosition = dropPreview.Position;
        }

        if (destinationSide.HasValue &&
            destinationPosition.HasValue &&
            IsLegalDropTarget(destinationSide.Value, destinationPosition.Value))
        {
            if (destinationPosition.Value != draggedTroop.Position)
            {
                battleState = battleState.MoveTroop(movedSide, movedTroopName, destinationPosition.Value);
                RebuildSetupLog($"{movedTroopName} moved.");
            }
        }

        ClearTroopDrag();
        RefreshFromState();
    }

    public void CancelTroopDrag()
    {
        ClearTroopDrag();
        RefreshFromState();
    }

    [RelayCommand]
    private void ResetBattle()
    {
        battleState = BattleState.CreateDefault();
        BattleHasStarted = false;
        ClearTroopDrag();
        SelectedBattleLogEntry = null;
        RebuildSetupLog("Battle reset.");
        RefreshFromState();
    }

    private void RefreshFromState()
    {
        LeftUnitName = battleState.LeftUnit.Name;
        RightUnitName = battleState.RightUnit.Name;
        RoundLabel = $"Round {battleState.RoundNumber}";

        ReplaceCells(LeftGridCells, battleState.LeftUnit);
        ReplaceCells(RightGridCells, battleState.RightUnit);
    }

    private void ReplaceCells(ObservableCollection<GridCellViewModel> target, Unit unit)
    {
        target.Clear();

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                var troop = unit.Troops.FirstOrDefault(candidate =>
                    candidate.Position.Row == row && candidate.Position.Column == column);

                var side = unit == battleState.LeftUnit ? BattleSide.Left : BattleSide.Right;
                var position = new GridPosition(row, column);

                target.Add(troop is null
                    ? ToEmptyCell(side, position)
                    : ToCell(troop, unit));
            }
        }
    }

    private GridCellViewModel ToCell(Troop troop, Unit unit)
    {
        var side = unit == battleState.LeftUnit ? BattleSide.Left : BattleSide.Right;
        var isDraggedSource = draggedTroop?.Side == side &&
            draggedTroop.Position == troop.Position &&
            draggedTroop.Name == troop.Name;

        if (isDraggedSource)
        {
            return ToEmptyCell(side, troop.Position);
        }

        var hp = $"{troop.CurrentHitPoints}/{troop.Stats.MaxHitPoints} HP";
        var attacks = $"{troop.RemainingBattleAttacks}/{troop.MaxBattleAttacks} attacks";
        var isActor = SelectedBattleLogEntry?.ActorSide == side && SelectedBattleLogEntry.ActorPosition == troop.Position;
        var isTarget = SelectedBattleLogEntry?.TargetSide == side && SelectedBattleLogEntry.TargetPosition == troop.Position;

        var borderBrush = GetSelectionBorderBrush(isActor, isTarget, SelectedBattleLogEntry?.Intent);
        var borderThickness = new Thickness(isActor || isTarget ? 4 : 1);

        return new GridCellViewModel(
            side,
            troop.Position,
            true,
            troop.Name,
            troop.ClassDefinition.DisplayName,
            troop.ClassDefinition.PortraitAssetPath,
            GetPortraitImage(troop.ClassDefinition.PortraitAssetPath),
            !string.IsNullOrWhiteSpace(troop.ClassDefinition.PortraitAssetPath),
            hp,
            attacks,
            GetCellBackground(side, troop.Position),
            borderBrush,
            borderThickness);
    }

    private GridCellViewModel ToEmptyCell(BattleSide side, GridPosition position)
    {
        return new GridCellViewModel(
            side,
            position,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            false,
            string.Empty,
            string.Empty,
            GetCellBackground(side, position),
            Brushes.Gray,
            new Thickness(1));
    }

    private IImage? GetPortraitImage(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        if (portraitCache.TryGetValue(assetPath, out var cachedImage))
        {
            return cachedImage;
        }

        // XAML does not reliably convert bound URI strings into bitmap sources, so the App layer loads
        // the Avalonia resource while Domain only provides a plain asset path.
        using var stream = AssetLoader.Open(new Uri(assetPath));
        var image = new Bitmap(stream);
        portraitCache[assetPath] = image;
        return image;
    }

    private void AddSetupEventsToLog()
    {
        foreach (var setupEvent in battleState.CreateSetupEvents())
        {
            BattleLog.Add(ToLogEntry(setupEvent));
        }
    }

    private void RebuildSetupLog(string firstLine)
    {
        BattleLog.Clear();
        BattleLog.Add(new BattleLogEntryViewModel(firstLine));
        AddSetupEventsToLog();
    }

    private void RotateFormation(BattleSide side, string logLine)
    {
        if (!CanRotateFormations)
        {
            return;
        }

        battleState = battleState.RotateFormationClockwise(side);
        ClearTroopDrag();
        SelectedBattleLogEntry = null;
        RebuildSetupLog(logLine);
        RefreshFromState();
    }

    private void UpdateDraggedPortraitPosition(Point pointerPosition)
    {
        DraggedPortraitLeft = pointerPosition.X - 24;
        DraggedPortraitTop = pointerPosition.Y - 24;
    }

    private void ClearTroopDrag()
    {
        draggedTroop = null;
        dropPreview = null;
        IsDraggingTroop = false;
        DraggedPortraitImage = null;
    }

    private bool IsLegalDropTarget(BattleSide side, GridPosition position)
    {
        if (draggedTroop is null ||
            !CanRotateFormations ||
            side != draggedTroop.Side)
        {
            return false;
        }

        return !battleState.GetUnit(side).Troops.Any(troop =>
            troop.Position == position &&
            (troop.Name != draggedTroop.Name || troop.Position != draggedTroop.Position));
    }

    private IBrush GetCellBackground(BattleSide side, GridPosition position)
    {
        if (dropPreview is not null && dropPreview.Side == side && dropPreview.Position == position)
        {
            return dropPreview.IsLegal ? LegalDropBackground : IllegalDropBackground;
        }

        return side == BattleSide.Left ? LeftCellBackground : RightCellBackground;
    }

    private static BattleLogEntryViewModel ToLogEntry(BattleEvent battleEvent)
    {
        return new BattleLogEntryViewModel(
            battleEvent.Description,
            battleEvent.ActorSide,
            battleEvent.ActorPosition,
            battleEvent.TargetSide,
            battleEvent.TargetPosition,
            battleEvent.Intent);
    }

    private static IBrush GetAttackArrowBrush(BattleEventIntent intent)
    {
        return intent == BattleEventIntent.Helpful ? Brushes.LimeGreen : Brushes.Red;
    }

    private static IBrush GetSelectionBorderBrush(bool isActor, bool isTarget, BattleEventIntent? intent)
    {
        if (isTarget && intent == BattleEventIntent.Helpful)
        {
            return Brushes.DeepSkyBlue;
        }

        if (isActor)
        {
            return Brushes.Lime;
        }

        if (isTarget)
        {
            return Brushes.Yellow;
        }

        return Brushes.Gray;
    }

    private static string CreateArrowPathData(Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length <= 0.01)
        {
            return string.Empty;
        }

        var unitX = dx / length;
        var unitY = dy / length;
        var perpendicularX = -unitY;
        var perpendicularY = unitX;

        var leftHead = new Point(
            end.X - unitX * ArrowHeadLength + perpendicularX * ArrowHeadWidth,
            end.Y - unitY * ArrowHeadLength + perpendicularY * ArrowHeadWidth);

        var rightHead = new Point(
            end.X - unitX * ArrowHeadLength - perpendicularX * ArrowHeadWidth,
            end.Y - unitY * ArrowHeadLength - perpendicularY * ArrowHeadWidth);

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"M {start.X:0.###},{start.Y:0.###} L {end.X:0.###},{end.Y:0.###} M {leftHead.X:0.###},{leftHead.Y:0.###} L {end.X:0.###},{end.Y:0.###} L {rightHead.X:0.###},{rightHead.Y:0.###}");
    }

    private sealed record DraggedTroop(BattleSide Side, GridPosition Position, string Name, IImage? PortraitImage);

    private sealed record DropPreview(BattleSide Side, GridPosition Position, bool IsLegal);
}

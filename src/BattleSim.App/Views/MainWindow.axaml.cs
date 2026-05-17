using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using BattleSim.App.ViewModels;
using BattleSim.Domain.Models;
using BattleSim.Engine;

namespace BattleSim.App.Views;

public sealed partial class MainWindow : Window
{
    private const int GridSize = 3;
    private int arrowRequestVersion;
    private bool isDraggingTroop;

    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainWindowViewModel();
        viewModel.AttackArrowRequested += OnAttackArrowRequested;
        DataContext = viewModel;
    }

    private async void OnAttackArrowRequested(BattleLogEntryViewModel attack)
    {
        if (!attack.IsAttack || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var requestVersion = ++arrowRequestVersion;

        // Let the refreshed grid measure before translating formation cells into overlay coordinates.
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

        if (requestVersion != arrowRequestVersion ||
            viewModel.SelectedBattleLogEntry != attack ||
            !TryGetCellCenter(attack.ActorSide!.Value, attack.ActorPosition!.Value, out var start) ||
            !TryGetCellCenter(attack.TargetSide!.Value, attack.TargetPosition!.Value, out var end))
        {
            return;
        }

        viewModel.ShowAttackArrow(start, end, attack.Intent);
    }

    private void OnGridCellPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            sender is not Control control ||
            control.DataContext is not GridCellViewModel cell)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(BattleGridLayer);
        if (!pointer.Properties.IsLeftButtonPressed || !viewModel.CanBeginTroopDrag(cell))
        {
            return;
        }

        isDraggingTroop = true;
        e.Pointer.Capture(control);
        viewModel.BeginTroopDrag(cell, pointer.Position);
        e.Handled = true;
    }

    private void OnGridCellPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isDraggingTroop || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var pointerPosition = e.GetPosition(BattleGridLayer);
        var hasHoverCell = TryGetCellAtPoint(pointerPosition, out var side, out var position);
        viewModel.UpdateTroopDrag(
            pointerPosition,
            hasHoverCell ? side : null,
            hasHoverCell ? position : null);
        e.Handled = true;
    }

    private void OnGridCellPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isDraggingTroop || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        isDraggingTroop = false;
        e.Pointer.Capture(null);

        var pointerPosition = e.GetPosition(BattleGridLayer);
        if (TryGetCellAtPoint(pointerPosition, out var side, out var position))
        {
            viewModel.CompleteTroopDrag(side, position);
        }
        else
        {
            viewModel.CompleteTroopDrag(null, null);
        }

        e.Handled = true;
    }

    private bool TryGetCellCenter(BattleSide side, GridPosition position, out Point center)
    {
        var formationGrid = side == BattleSide.Left ? LeftFormationGrid : RightFormationGrid;
        var cellWidth = formationGrid.Bounds.Width / GridSize;
        var cellHeight = formationGrid.Bounds.Height / GridSize;

        center = default;

        if (cellWidth <= 0 || cellHeight <= 0)
        {
            return false;
        }

        var localCenter = new Point(
            (position.Column + 0.5) * cellWidth,
            (position.Row + 0.5) * cellHeight);

        var translatedCenter = formationGrid.TranslatePoint(localCenter, BattleGridLayer);
        if (translatedCenter is null)
        {
            return false;
        }

        center = translatedCenter.Value;
        return true;
    }

    private bool TryGetCellAtPoint(Point point, out BattleSide side, out GridPosition position)
    {
        if (TryGetCellAtPoint(LeftFormationGrid, BattleSide.Left, point, out side, out position))
        {
            return true;
        }

        return TryGetCellAtPoint(RightFormationGrid, BattleSide.Right, point, out side, out position);
    }

    private bool TryGetCellAtPoint(
        Control formationGrid,
        BattleSide formationSide,
        Point point,
        out BattleSide side,
        out GridPosition position)
    {
        side = formationSide;
        position = default;

        var origin = formationGrid.TranslatePoint(new Point(0, 0), BattleGridLayer);
        if (origin is null || formationGrid.Bounds.Width <= 0 || formationGrid.Bounds.Height <= 0)
        {
            return false;
        }

        var localX = point.X - origin.Value.X;
        var localY = point.Y - origin.Value.Y;

        if (localX < 0 ||
            localY < 0 ||
            localX > formationGrid.Bounds.Width ||
            localY > formationGrid.Bounds.Height)
        {
            return false;
        }

        var column = Math.Min(GridSize - 1, (int)(localX / (formationGrid.Bounds.Width / GridSize)));
        var row = Math.Min(GridSize - 1, (int)(localY / (formationGrid.Bounds.Height / GridSize)));
        position = new GridPosition(row, column);
        return true;
    }
}

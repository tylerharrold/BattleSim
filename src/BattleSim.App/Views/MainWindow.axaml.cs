using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using BattleSim.App.ViewModels;
using BattleSim.Domain.Models;
using BattleSim.Engine;

namespace BattleSim.App.Views;

public sealed partial class MainWindow : Window
{
    private const int GridSize = 3;
    private int arrowRequestVersion;

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
}

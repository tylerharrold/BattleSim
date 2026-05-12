using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using BattleSim.Domain.Models;
using BattleSim.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BattleSim.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    // The view model adapts engine state into bindable UI data without owning combat rules.
    private readonly BattleEngine battleEngine = new();

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

    public bool CanRotateFormations => !BattleHasStarted;

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

    [RelayCommand]
    private void ResetBattle()
    {
        battleState = BattleState.CreateDefault();
        BattleHasStarted = false;
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

                target.Add(troop is null ? GridCellViewModel.Empty : ToCell(troop, unit));
            }
        }
    }

    private GridCellViewModel ToCell(Troop troop, Unit unit)
    {
        var hp = $"{troop.CurrentHitPoints}/{troop.Stats.MaxHitPoints} HP";
        var attacks = $"{troop.RemainingBattleAttacks}/{troop.MaxBattleAttacks} attacks";
        var side = unit == battleState.LeftUnit ? BattleSide.Left : BattleSide.Right;
        var isActor = SelectedBattleLogEntry?.ActorSide == side && SelectedBattleLogEntry.ActorPosition == troop.Position;
        var isTarget = SelectedBattleLogEntry?.TargetSide == side && SelectedBattleLogEntry.TargetPosition == troop.Position;

        var borderBrush = isActor ? Brushes.Lime : isTarget ? Brushes.Yellow : Brushes.Gray;
        var borderThickness = new Thickness(isActor || isTarget ? 4 : 1);

        return new GridCellViewModel(troop.Name, troop.ClassDefinition.DisplayName, hp, attacks, borderBrush, borderThickness);
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
        SelectedBattleLogEntry = null;
        RebuildSetupLog(logLine);
        RefreshFromState();
    }

    private static BattleLogEntryViewModel ToLogEntry(BattleEvent battleEvent)
    {
        return new BattleLogEntryViewModel(
            battleEvent.Description,
            battleEvent.ActorSide,
            battleEvent.ActorPosition,
            battleEvent.TargetSide,
            battleEvent.TargetPosition);
    }
}

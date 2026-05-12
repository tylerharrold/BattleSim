using System.Collections.ObjectModel;
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
        BattleLog.Add("Battle initialized.");
        AddSetupEventsToLog();
        RefreshFromState();
    }

    public ObservableCollection<GridCellViewModel> LeftGridCells { get; } = new();

    public ObservableCollection<GridCellViewModel> RightGridCells { get; } = new();

    public ObservableCollection<string> BattleLog { get; } = new();

    [ObservableProperty]
    private string leftUnitName = string.Empty;

    [ObservableProperty]
    private string rightUnitName = string.Empty;

    [ObservableProperty]
    private string roundLabel = string.Empty;

    [ObservableProperty]
    private string? selectedBattleLogEntry;

    [RelayCommand]
    private void RunNextAttack()
    {
        ApplyStepResult(battleEngine.RunNextAttack(battleState));
    }

    [RelayCommand]
    private void RunNextTurn()
    {
        ApplyStepResult(battleEngine.RunNextTurn(battleState));
    }

    [RelayCommand]
    private void RunOneRound()
    {
        ApplyStepResult(battleEngine.RunOneRound(battleState));
    }

    private void ApplyStepResult(BattleStepResult result)
    {
        battleState = result.State;

        foreach (var battleEvent in result.Events)
        {
            BattleLog.Add(battleEvent.Description);
        }

        if (battleState.IsComplete)
        {
            BattleLog.Add("Battle complete.");
        }

        RefreshFromState();
    }

    [RelayCommand]
    private void ResetBattle()
    {
        battleState = BattleState.CreateDefault();
        BattleLog.Clear();
        SelectedBattleLogEntry = null;
        BattleLog.Add("Battle reset.");
        AddSetupEventsToLog();
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

    private static void ReplaceCells(ObservableCollection<GridCellViewModel> target, Unit unit)
    {
        target.Clear();

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                var troop = unit.Troops.FirstOrDefault(candidate =>
                    candidate.Position.Row == row && candidate.Position.Column == column);

                target.Add(troop is null ? GridCellViewModel.Empty : ToCell(troop));
            }
        }
    }

    private static GridCellViewModel ToCell(Troop troop)
    {
        var hp = $"{troop.CurrentHitPoints}/{troop.Stats.MaxHitPoints} HP";
        return new GridCellViewModel(troop.Name, troop.TroopClass.ToString(), hp);
    }

    private void AddSetupEventsToLog()
    {
        foreach (var setupEvent in battleState.CreateSetupEvents())
        {
            BattleLog.Add(setupEvent.Description);
        }
    }
}

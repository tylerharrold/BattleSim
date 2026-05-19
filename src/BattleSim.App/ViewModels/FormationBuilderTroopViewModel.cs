using BattleSim.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BattleSim.App.ViewModels;

public sealed partial class FormationBuilderTroopViewModel : ObservableObject
{
    private readonly Action onChanged;

    public FormationBuilderTroopViewModel(
        string slotId,
        string name,
        TroopClassDefinition classDefinition,
        int row,
        int column,
        bool isLeader,
        Action onChanged)
    {
        this.onChanged = onChanged;
        SlotId = slotId;
        this.name = name;
        ClassDefinition = classDefinition;
        this.row = row;
        this.column = column;
        this.isLeader = isLeader;
    }

    public string SlotId { get; }

    public TroopClassDefinition ClassDefinition { get; }

    public string ClassName => ClassDefinition.DisplayName;

    public string TroopClassId => ClassDefinition.Id;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private int row;

    [ObservableProperty]
    private int column;

    [ObservableProperty]
    private bool isLeader;

    partial void OnNameChanged(string value) => onChanged();

    partial void OnRowChanged(int value) => onChanged();

    partial void OnColumnChanged(int value) => onChanged();

    partial void OnIsLeaderChanged(bool value) => onChanged();
}

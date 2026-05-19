using BattleSim.Domain.Models;

namespace BattleSim.App.ViewModels;

public sealed class TroopClassOptionViewModel
{
    public TroopClassOptionViewModel(TroopClassDefinition definition)
    {
        Definition = definition;
    }

    public TroopClassDefinition Definition { get; }

    public string Id => Definition.Id;

    public string DisplayName => Definition.DisplayName;

    public override string ToString() => DisplayName;
}

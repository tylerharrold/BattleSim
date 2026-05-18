using BattleSim.Domain.Models;

namespace BattleSim.App.ViewModels;

public sealed class UnitTemplateOptionViewModel
{
    public UnitTemplateOptionViewModel(UnitTemplate template)
    {
        Template = template;
    }

    public UnitTemplate Template { get; }

    public string Id => Template.Id;

    public string Name => Template.Name;

    public override string ToString() => Name;
}

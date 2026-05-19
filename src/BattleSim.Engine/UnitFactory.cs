using BattleSim.Domain.Models;

namespace BattleSim.Engine;

public sealed class UnitFactory
{
    private readonly IReadOnlyDictionary<string, TroopClassDefinition> classDefinitions;

    public UnitFactory(IReadOnlyDictionary<string, TroopClassDefinition> classDefinitions)
    {
        this.classDefinitions = classDefinitions;
    }

    public Unit Create(UnitTemplate template, BattleSide side)
    {
        UnitTemplateValidator.ValidateAndThrow(template, classDefinitions);

        var troops = template.Troops
            .Select(templateTroop =>
            {
                var classDefinition = classDefinitions[templateTroop.TroopClassId];
                return new Troop(
                    templateTroop.Name,
                    classDefinition,
                    ToRuntimePosition(templateTroop, side));
            })
            .ToArray();

        // Templates may omit a leader. Runtime targeting treats a missing leader as
        // "no leader target available" and falls back to normal rules safely.
        var leaderName = template.Troops.FirstOrDefault(troop => troop.IsLeader)?.Name;

        return new Unit(template.Name, troops, leaderName);
    }

    public static GridPosition ToRuntimePosition(UnitTemplateTroop troop, BattleSide side)
    {
        // Templates are authored facing north with row 0 as the front. In battle, the front
        // rotates toward the opposing unit: Blue faces global right, Red faces global left.
        return side == BattleSide.Left
            ? new GridPosition(troop.Column, 2 - troop.Row)
            : new GridPosition(2 - troop.Column, troop.Row);
    }
}

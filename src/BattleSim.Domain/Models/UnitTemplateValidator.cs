namespace BattleSim.Domain.Models;

public static class UnitTemplateValidator
{
    public static void ValidateAndThrow(
        UnitTemplate template,
        IReadOnlyDictionary<string, TroopClassDefinition> classDefinitions)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(classDefinitions);

        if (string.IsNullOrWhiteSpace(template.Id))
        {
            throw new UnitTemplateValidationException("Unit template id is required.");
        }

        if (string.IsNullOrWhiteSpace(template.Name))
        {
            throw new UnitTemplateValidationException($"Unit template '{template.Id}' must have a name.");
        }

        if (template.Troops.Count is < 1 or > 5)
        {
            throw new UnitTemplateValidationException($"Unit template '{template.Id}' must contain between 1 and 5 troops.");
        }

        var positions = new HashSet<GridPosition>();
        var leaderCount = 0;
        var slotIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var troop in template.Troops)
        {
            if (string.IsNullOrWhiteSpace(troop.SlotId))
            {
                throw new UnitTemplateValidationException($"Unit template '{template.Id}' contains a troop without a slot id.");
            }

            if (!slotIds.Add(troop.SlotId))
            {
                throw new UnitTemplateValidationException($"Unit template '{template.Id}' contains duplicate slot id '{troop.SlotId}'.");
            }

            if (string.IsNullOrWhiteSpace(troop.Name))
            {
                throw new UnitTemplateValidationException($"Unit template '{template.Id}' contains troop '{troop.SlotId}' without a display name.");
            }

            if (!classDefinitions.ContainsKey(troop.TroopClassId))
            {
                throw new UnitTemplateValidationException($"Unit template '{template.Id}' references unknown troop class '{troop.TroopClassId}'.");
            }

            var position = new GridPosition(troop.Row, troop.Column);
            if (!position.IsInFormation)
            {
                throw new UnitTemplateValidationException($"Unit template '{template.Id}' places troop '{troop.SlotId}' outside the 3x3 formation.");
            }

            if (!positions.Add(position))
            {
                throw new UnitTemplateValidationException($"Unit template '{template.Id}' has more than one troop in row {troop.Row}, column {troop.Column}.");
            }

            if (troop.IsLeader)
            {
                leaderCount++;
            }
        }

        if (leaderCount > 1)
        {
            throw new UnitTemplateValidationException($"Unit template '{template.Id}' may only have one leader.");
        }
    }
}

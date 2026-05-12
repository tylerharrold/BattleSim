namespace BattleSim.Domain.Models;

public sealed class Unit
{
    public Unit(string name, IEnumerable<Troop> troops)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Unit name is required.", nameof(name));
        }

        Name = name;
        Troops = troops.ToList();
    }

    public string Name { get; }

    public IReadOnlyList<Troop> Troops { get; }

    public bool IsDefeated => Troops.All(troop => troop.IsDefeated);

    public IEnumerable<Troop> LivingTroops => Troops.Where(troop => !troop.IsDefeated);

    public Unit Clone() => new(Name, Troops.Select(troop => troop.Clone()));
}

using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public sealed class Unit
{
    public Unit(
        string name,
        IEnumerable<Troop> troops,
        string? leaderName = null,
        TargetingPreference targetingPreference = TargetingPreference.Normal)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Unit name is required.", nameof(name));
        }

        Name = name;
        Troops = troops.ToList();
        LeaderName = leaderName;
        TargetingPreference = targetingPreference;
    }

    public string Name { get; }

    public IReadOnlyList<Troop> Troops { get; }

    public string? LeaderName { get; }

    public TargetingPreference TargetingPreference { get; }

    public bool IsDefeated => Troops.All(troop => troop.IsDefeated);

    public IEnumerable<Troop> LivingTroops => Troops.Where(troop => !troop.IsDefeated);

    public bool IsLeader(Troop troop) => troop.Name == LeaderName;

    public Unit WithTargetingPreference(TargetingPreference targetingPreference)
    {
        return new Unit(Name, Troops.Select(troop => troop.Clone()), LeaderName, targetingPreference);
    }

    public Unit Clone() => new(Name, Troops.Select(troop => troop.Clone()), LeaderName, TargetingPreference);
}

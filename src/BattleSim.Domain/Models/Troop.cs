using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public sealed class Troop
{
    public Troop(string name, TroopClass troopClass, Stats stats, GridPosition position)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Troop name is required.", nameof(name));
        }

        if (!position.IsInFormation)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Troop positions must fit inside a 3x3 formation.");
        }

        Name = name;
        TroopClass = troopClass;
        Stats = stats;
        Position = position;
        CurrentHitPoints = stats.MaxHitPoints;
    }

    public string Name { get; }

    public TroopClass TroopClass { get; }

    public Stats Stats { get; }

    public GridPosition Position { get; }

    // Domain state stays UI-agnostic; presentation concerns such as colors and labels are mapped in the app layer.
    public int CurrentHitPoints { get; private set; }

    public bool IsDefeated => CurrentHitPoints <= 0;

    public void TakeDamage(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");
        }

        CurrentHitPoints = Math.Max(0, CurrentHitPoints - amount);
    }

    public Troop Clone()
    {
        var clone = new Troop(Name, TroopClass, Stats, Position);
        clone.CurrentHitPoints = CurrentHitPoints;
        return clone;
    }
}

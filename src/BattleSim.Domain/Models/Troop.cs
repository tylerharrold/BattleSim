using BattleSim.Domain.Enums;

namespace BattleSim.Domain.Models;

public sealed class Troop
{
    public Troop(string name, TroopClass troopClass, Stats stats, GridPosition position)
        : this(name, BuiltInTroopClasses.FromLegacyEnum(troopClass) with { BaseStats = stats }, position)
    {
    }

    public Troop(string name, TroopClassDefinition classDefinition, GridPosition position)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Troop name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(classDefinition);

        if (!position.IsInFormation)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Troop positions must fit inside a 3x3 formation.");
        }

        Name = name;
        ClassDefinition = classDefinition;
        Stats = classDefinition.BaseStats;
        Position = position;
        CurrentHitPoints = Stats.MaxHitPoints;
    }

    public string Name { get; }

    public TroopClassDefinition ClassDefinition { get; }

    public Stats Stats { get; }

    public GridPosition Position { get; }

    // Domain state stays UI-agnostic; presentation concerns such as colors and labels are mapped in the app layer.
    public int CurrentHitPoints { get; private set; }

    public int MaxBattleAttacks { get; private set; }

    public int RemainingBattleAttacks { get; private set; }

    public bool IsDefeated => CurrentHitPoints <= 0;

    public bool CanAttack => !IsDefeated && RemainingBattleAttacks > 0;

    public void SetBattleAttackLimit(int attackCount)
    {
        if (attackCount is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(attackCount), "Battle attack counts must be between 1 and 3.");
        }

        MaxBattleAttacks = attackCount;
        RemainingBattleAttacks = attackCount;
    }

    public void SpendBattleAttack()
    {
        if (RemainingBattleAttacks <= 0)
        {
            throw new InvalidOperationException($"{Name} has no battle attacks remaining.");
        }

        RemainingBattleAttacks--;
    }

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
        return CloneAtPosition(Position);
    }

    public Troop CloneAtPosition(GridPosition position)
    {
        var clone = new Troop(Name, ClassDefinition, position);
        clone.CurrentHitPoints = CurrentHitPoints;
        clone.MaxBattleAttacks = MaxBattleAttacks;
        clone.RemainingBattleAttacks = RemainingBattleAttacks;
        return clone;
    }
}

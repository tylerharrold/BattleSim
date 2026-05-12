namespace BattleSim.Domain.Models;

public sealed record Stats(int MaxHitPoints, int Attack, int Defense, int Speed)
{
    public static Stats Default => new(MaxHitPoints: 20, Attack: 6, Defense: 2, Speed: 5);
}

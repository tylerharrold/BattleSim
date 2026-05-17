namespace BattleSim.Domain.Models;

public sealed record Stats(
    int MaxHitPoints,
    int Strength,
    int Defense,
    int Speed,
    int Faith,
    int Wisdom,
    int Dexterity,
    int Luck)
{
    public static Stats Default => new(
        MaxHitPoints: 20,
        Strength: 6,
        Defense: 2,
        Speed: 5,
        Faith: 5,
        Wisdom: 5,
        Dexterity: 5,
        Luck: 5);
}

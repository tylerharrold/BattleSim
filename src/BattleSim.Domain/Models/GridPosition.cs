namespace BattleSim.Domain.Models;

public readonly record struct GridPosition(int Row, int Column)
{
    public bool IsInFormation => Row is >= 0 and < 3 && Column is >= 0 and < 3;
}

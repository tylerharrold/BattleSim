namespace BattleSim.Engine;

public sealed record CombatRollResult(
    bool DidHit,
    bool WasLuckReroll,
    bool WasCritical,
    int BaseDamage,
    int CriticalBonusDamage)
{
    public int TotalDamage => BaseDamage + CriticalBonusDamage;
}

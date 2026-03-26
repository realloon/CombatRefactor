using CombatRefactor.Type;

namespace CombatRefactor;

public class CompProperties_FireSelector : CompProperties {
    public FireMode defaultMode = FireMode.Burst;
    public int burstShotCountOverride;
    public float autoBurstMultiplier = 2f;
    public int autoBurstShotCountCap;

    public CompProperties_FireSelector() => compClass = typeof(CompFireSelector);
}
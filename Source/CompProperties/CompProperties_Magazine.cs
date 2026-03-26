// ReSharper disable InconsistentNaming

namespace CombatRefactor;

public class CompProperties_Magazine : CompProperties {
    public int magazineCapacity;
    public int burstShotCountCapacityMultiplier = 10;

    public CompProperties_Magazine() => compClass = typeof(CompMagazine);
}
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace CombatRefactor;

public class CompProperties_Magazine : CompProperties {
    [UsedImplicitly]
    public readonly int magazineCapacity;

    [UsedImplicitly]
    public readonly int burstShotCountCapacityMultiplier = 10;

    [UsedImplicitly]
    public readonly int reloadTicks = 90;

    public CompProperties_Magazine() => compClass = typeof(CompMagazine);
}
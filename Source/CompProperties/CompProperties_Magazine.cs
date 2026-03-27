using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace CombatRefactor;

public class CompProperties_Magazine : CompProperties {
    [UsedImplicitly]
    public readonly int magazineCapacity; // Default: burstShotCount * 10 when omitted.

    [UsedImplicitly]
    public readonly int reloadTicks = 90;

    public CompProperties_Magazine() => compClass = typeof(CompMagazine);
}
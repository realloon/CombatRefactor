using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace CombatRefactor;

public class CompProperties_FireSelector : CompProperties {
    [UsedImplicitly]
    public readonly int autoBurstShotCount;

    public CompProperties_FireSelector() => compClass = typeof(CompFireSelector);
}

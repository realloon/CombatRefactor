// ReSharper disable InconsistentNaming

namespace CombatRefactor;

[DefOf]
public static class JobDefOf {
    public static readonly JobDef CRTeam_ReloadMagazine = null!;
    public static readonly JobDef CRTeam_SwitchFireMode = null!;

    static JobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
}

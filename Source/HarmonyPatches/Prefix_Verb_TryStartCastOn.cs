using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new[] {
    typeof(LocalTargetInfo),
    typeof(LocalTargetInfo),
    typeof(bool),
    typeof(bool),
    typeof(bool),
    typeof(bool)
})]
public static class Prefix_Verb_TryStartCastOn {
    [UsedImplicitly]
    public static bool Prefix(Verb __instance, LocalTargetInfo castTarg, bool canHitNonTargetPawns,
        ref bool __result) {
        #if DEBUG
        using var _ = PerformanceProfiler.Measure("Patch.Verb.TryStartCastOn");
        #endif

        if (__instance is not Verb_LaunchProjectile launchProjectile) {
            return true;
        }

        if (!canHitNonTargetPawns) {
            return true;
        }

        var caster = launchProjectile.caster;
        if (caster?.Map == null || caster.Faction == null) {
            return true;
        }

        if (!launchProjectile.TryFindShootLineFromTo(caster.Position, castTarg, out var resultingLine)) {
            return true;
        }

        var usedTarget = new LocalTargetInfo(resultingLine.Dest);
        if (!ProjectileCoverUtility.HasFriendlyPawnBlocker(caster, caster.Map, resultingLine.Source, usedTarget,
                castTarg)) {
            return true;
        }

        __result = false;
        return false;
    }
}
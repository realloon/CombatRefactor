using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Stance_Warmup), nameof(Stance_Warmup.StanceDraw))]
public static class Patch_Stance_Warmup_StanceDraw {
    [UsedImplicitly]
    public static void Postfix(Stance_Warmup __instance) {
        using var _ = PerformanceProfiler.Measure("Patch.Stance_Warmup.StanceDraw");

        if (!Find.Selector.IsSelected(__instance.stanceTracker.pawn)) {
            return;
        }

        if (__instance.verb is not Verb_LaunchProjectile launchProjectile) {
            return;
        }

        ProjectileSpreadVisualizationUtility.DrawSpreadBounds(launchProjectile, __instance.focusTarg);
    }
}

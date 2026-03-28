using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Verb), nameof(Verb.DrawHighlight))]
public static class Patch_Verb_DrawHighlight {
    [UsedImplicitly]
    public static void Postfix(Verb __instance, LocalTargetInfo target) {
        using var _ = PerformanceProfiler.Measure("Patch.Verb.DrawHighlight");

        if (__instance is not Verb_LaunchProjectile launchProjectile) {
            return;
        }

        ProjectileSpreadVisualizationUtility.DrawSpreadBounds(launchProjectile, target);
    }
}

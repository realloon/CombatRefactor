using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Verb), nameof(Verb.DrawHighlight))]
public static class Patch_Verb_DrawHighlight {
    public static void Postfix(Verb __instance, LocalTargetInfo target) {
        if (__instance is not Verb_LaunchProjectile launchProjectile) {
            return;
        }

        ProjectileSpreadVisualizationUtility.DrawSpreadBounds(launchProjectile, target);
    }
}

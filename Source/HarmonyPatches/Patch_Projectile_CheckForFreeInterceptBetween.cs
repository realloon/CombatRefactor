using CombatRefactor.Utility;
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Projectile), "CheckForFreeInterceptBetween")]
public static class Patch_Projectile_CheckForFreeInterceptBetween {
    [UsedImplicitly]
    public static bool Prefix(Projectile __instance, Vector3 lastExactPos, Vector3 newExactPos, ref bool __result) {
        if (__instance.def.projectile.flyOverhead) {
            return true;
        }

        __result = ProjectileCoverUtility.TryInterceptCoverBetween(__instance, lastExactPos, newExactPos);
        return false;
    }
}

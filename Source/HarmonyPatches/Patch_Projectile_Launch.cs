using CombatRefactor.Utility;
using JetBrains.Annotations;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Projectile), nameof(Projectile.Launch), new[] {
    typeof(Thing),
    typeof(Vector3),
    typeof(LocalTargetInfo),
    typeof(LocalTargetInfo),
    typeof(ProjectileHitFlags),
    typeof(bool),
    typeof(Thing),
    typeof(ThingDef)
})]
public static class Patch_Projectile_Launch {
    [UsedImplicitly]
    public static void Postfix(Projectile __instance) {
        ProjectileCoverUtility.InitializeForLaunch(__instance);
    }
}

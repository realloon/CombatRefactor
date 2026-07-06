using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Projectile), "ImpactSomething")]
public static class Prefix_Projectile_ImpactSomething {
    [UsedImplicitly]
    public static bool Prefix(Projectile __instance) {
        if (__instance.def.projectile.flyOverhead) return true;

        return !ProjectileCoverUtility.TryHandleDirectTargetImpact(__instance);
    }
}
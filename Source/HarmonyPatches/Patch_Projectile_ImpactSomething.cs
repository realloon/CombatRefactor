using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Projectile), "ImpactSomething")]
public static class Patch_Projectile_ImpactSomething {
    [UsedImplicitly]
    public static bool Prefix(Projectile __instance) => !ProjectileCoverUtility.TryHandleLeanTargetImpact(__instance);
}
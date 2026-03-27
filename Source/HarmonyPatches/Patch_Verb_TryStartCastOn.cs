using CombatRefactor.Utility;
using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new[] {
    typeof(LocalTargetInfo),
    typeof(LocalTargetInfo),
    typeof(bool),
    typeof(bool),
    typeof(bool),
    typeof(bool),
})]
public static class Patch_Verb_TryStartCastOn {
    public static bool Prefix(Verb __instance, LocalTargetInfo castTarg, ref bool __result) {
        if (__instance is not Verb_LaunchProjectile launchProjectile) {
            return true;
        }

        var caster = launchProjectile.caster;
        if (caster?.Map == null || caster.Faction == null) {
            return true;
        }

        if (!launchProjectile.TryFindShootLineFromTo(caster.Position, castTarg, out var resultingLine)) {
            return true;
        }

        if (!ProjectileCoverUtility.HasFriendlyPawnBlocker(caster, caster.Map, resultingLine.Source, castTarg, castTarg)) {
            return true;
        }

        __result = false;
        return false;
    }
}

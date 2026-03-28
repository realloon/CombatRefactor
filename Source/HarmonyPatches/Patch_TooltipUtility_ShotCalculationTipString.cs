using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(TooltipUtility), nameof(TooltipUtility.ShotCalculationTipString))]
public static class Patch_TooltipUtility_ShotCalculationTipString {
    [UsedImplicitly]
    public static bool Prefix(Thing target, ref string __result) {
        if (!ProjectileReadoutUtility.TryBuildShotCalculationTipString(target, out var readout)) {
            return true;
        }

        __result = readout;
        return false;
    }
}

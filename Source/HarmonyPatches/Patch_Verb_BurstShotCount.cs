using JetBrains.Annotations;
using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Verb), nameof(Verb.BurstShotCount), MethodType.Getter)]
public static class Patch_Verb_BurstShotCount {
    [UsedImplicitly]
    public static void Postfix(Verb __instance, ref int __result) {
        var fireSelector = __instance.EquipmentSource?.GetComp<CompFireSelector>();
        if (fireSelector == null) return;

        __result = fireSelector.GetBurstShotCountFor(__result);
    }
}
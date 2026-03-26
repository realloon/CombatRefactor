using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Verb), nameof(Verb.Available))]
public static class Patch_Verb_Available {
    public static void Postfix(Verb __instance, ref bool __result) {
        if (!__result) {
            return;
        }

        var magazine = __instance.EquipmentSource?.GetComp<CompMagazine>();
        if (magazine == null) {
            return;
        }

        __result = !magazine.Empty;
    }
}
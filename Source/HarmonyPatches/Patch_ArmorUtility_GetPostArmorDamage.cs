using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage))]
public static class Patch_ArmorUtility_GetPostArmorDamage {
    [UsedImplicitly]
    public static bool Prefix(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part,
        ref DamageDef damageDef, ref bool deflectedByMetalArmor, ref bool diminishedByMetalArmor, ref float __result) {
        if (!ArmorPenetrationUtility.ShouldHandle(damageDef)) return true;

        __result = ArmorPenetrationUtility.GetPostArmorDamage(pawn, amount, armorPenetration, part, ref damageDef,
            out deflectedByMetalArmor, out diminishedByMetalArmor);

        return false;
    }
}
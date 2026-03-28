using HarmonyLib;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(PawnLeaner), nameof(PawnLeaner.ShouldLean))]
public static class Patch_PawnLeaner_ShouldLean {
    private static readonly AccessTools.FieldRef<PawnLeaner, Pawn> PawnRef =
        AccessTools.FieldRefAccess<PawnLeaner, Pawn>("pawn");

    private static readonly AccessTools.FieldRef<PawnLeaner, IntVec3> ShootSourceOffsetRef =
        AccessTools.FieldRefAccess<PawnLeaner, IntVec3>("shootSourceOffset");

    [UsedImplicitly]
    public static bool Prefix(PawnLeaner __instance, ref bool __result) {
        var pawn = PawnRef(__instance);
        if (pawn?.stances?.curStance is not Stance_Busy busyStance ||
            ShootSourceOffsetRef(__instance) == IntVec3.Zero) {
            __result = false;
            return false;
        }

        if (busyStance is Stance_Warmup or Stance_Cooldown { verb.state: VerbState.Bursting }) {
            __result = true;
            return false;
        }

        __result = false;
        return false;
    }
}
using JetBrains.Annotations;
using HarmonyLib;
using Verse.AI;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver_AttackStatic), "MakeNewToils")]
public static class Patch_JobDriver_AttackStatic_MakeNewToils {
    private static readonly AccessTools.FieldRef<JobDriver_AttackStatic, bool> StartedIncapacitatedField =
        AccessTools.FieldRefAccess<JobDriver_AttackStatic, bool>("startedIncapacitated");

    [UsedImplicitly]
    public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_AttackStatic __instance) {
        return WrapToils(__result, __instance);
    }

    private static IEnumerable<Toil> WrapToils(IEnumerable<Toil> toils, JobDriver_AttackStatic jobDriver) {
        var wrappedInitToil = false;

        foreach (var toil in toils) {
            if (!wrappedInitToil &&
                toil.defaultCompleteMode == ToilCompleteMode.Never &&
                toil.tickIntervalAction != null) {
                WrapInitAction(toil, jobDriver);
                wrappedInitToil = true;
            }

            yield return toil;
        }
    }

    private static void WrapInitAction(Toil toil, JobDriver_AttackStatic jobDriver) {
        var originalInitAction = toil.initAction;
        toil.initAction = () => {
            originalInitAction?.Invoke();

            if (SuspendedAttackJobStateUtility.TryConsume(jobDriver.job, out var startedIncapacitated)) {
                StartedIncapacitatedField(jobDriver) = startedIncapacitated;
            }
        };
    }
}
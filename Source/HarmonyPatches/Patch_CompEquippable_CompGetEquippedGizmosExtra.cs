using HarmonyLib;
using CombatRefactor.Type;

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(CompEquippable), nameof(CompEquippable.CompGetEquippedGizmosExtra))]
public static class Patch_CompEquippable_CompGetEquippedGizmosExtra {
    public static void Postfix(CompEquippable __instance, ref IEnumerable<Gizmo> __result) {
        __result = AppendEquippedGizmos(__result, __instance);
    }

    private static IEnumerable<Gizmo> AppendEquippedGizmos(IEnumerable<Gizmo> original, CompEquippable compEquippable) {
        foreach (var gizmo in original) {
            yield return gizmo;
        }

        if (compEquippable.parent == null) {
            yield break;
        }

        foreach (var comp in compEquippable.parent.AllComps) {
            if (comp is not IEquippedGizmoProvider gizmoProvider) {
                continue;
            }

            foreach (var gizmo in gizmoProvider.GetEquippedGizmos()) {
                yield return gizmo;
            }
        }
    }
}
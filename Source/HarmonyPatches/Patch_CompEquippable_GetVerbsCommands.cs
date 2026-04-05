using JetBrains.Annotations;
using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(CompEquippable), nameof(CompEquippable.GetVerbsCommands))]
public static class Patch_CompEquippable_GetVerbsCommands {
    [UsedImplicitly]
    public static void Postfix(CompEquippable __instance, ref IEnumerable<Command> __result) {
        __result = DecorateCommands(__instance, __result);
    }

    private static IEnumerable<Command> DecorateCommands(CompEquippable compEquippable, IEnumerable<Command> original) {
        var magazine = compEquippable.parent.GetComp<CompMagazine>();

        foreach (var command in original) {
            if (magazine != null && command is Command_VerbTarget) {
                command.defaultDescPostfix += "\n\n" +
                                              "CRTeam_MagazineStatus".Translate(magazine.RemainingShots,
                                                  magazine.MagazineCapacity);
                if (magazine.Empty) {
                    command.Disable("CRTeam_Disabled_MagazineEmpty".Translate());
                }
            }

            yield return command;
        }
    }
}
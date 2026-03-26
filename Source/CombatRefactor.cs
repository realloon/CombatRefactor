using JetBrains.Annotations;
using HarmonyLib;

namespace CombatRefactor;

[UsedImplicitly]
public class CombatRefactor {
    static CombatRefactor() {
        var harmony = new Harmony("CRTeam.CombatRefactor");
        harmony.PatchAll();
    }
}
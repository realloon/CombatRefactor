global using Verse;
global using RimWorld;
global using CombatRefactor.Type;
using JetBrains.Annotations;
using HarmonyLib;

namespace CombatRefactor;

[UsedImplicitly]
[StaticConstructorOnStartup]
public static class CombatRefactor {
    static CombatRefactor() {
        var harmony = new Harmony("CRTeam.CombatRefactor");
        harmony.PatchAll();

        InjectTestFireSelector();
    }

    private static void InjectTestFireSelector() {
        var assaultRifle = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_AssaultRifle");
        assaultRifle.comps.Add(new CompProperties_FireSelector());
        assaultRifle.comps.Add(new CompProperties_Magazine());
    }
}
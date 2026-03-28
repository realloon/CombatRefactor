global using Verse;
global using RimWorld;
global using CombatRefactor.Type;
using CombatRefactor.Utility;
using JetBrains.Annotations;
using HarmonyLib;

namespace CombatRefactor;

[UsedImplicitly]
[StaticConstructorOnStartup]
public static class CombatRefactor {
    static CombatRefactor() {
        var harmony = new Harmony("CRTeam.CombatRefactor");
        harmony.PatchAll();

        ProjectileCoverUtility.InjectProjectileStageComp();
        InjectTestFireSelector();

        #if DEBUG
        Log.Message("[CombatRefactor] DEBUG profiler enabled.");
        #endif
    }

    private static void InjectTestFireSelector() {
        var assaultRifle = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_AssaultRifle");
        assaultRifle.comps.Add(new CompProperties_FireSelector());
        assaultRifle.comps.Add(new CompProperties_Magazine());
    }
}
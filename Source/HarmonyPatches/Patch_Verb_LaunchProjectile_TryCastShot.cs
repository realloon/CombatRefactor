using JetBrains.Annotations;
using HarmonyLib;
using CombatRefactor.Utility;

// ReSharper disable InconsistentNaming

namespace CombatRefactor.HarmonyPatches;

[HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
public static class Patch_Verb_LaunchProjectile_TryCastShot {
    private static readonly AccessTools.FieldRef<Verb, int> LastShotTickRef =
        AccessTools.FieldRefAccess<Verb, int>("lastShotTick");

    private static readonly AccessTools.FieldRef<Verb, bool> CanHitNonTargetPawnsNowRef =
        AccessTools.FieldRefAccess<Verb, bool>("canHitNonTargetPawnsNow");

    private static readonly Func<Verb_LaunchProjectile, float, IntVec3> GetForcedMissTarget =
        AccessTools.MethodDelegate<Func<Verb_LaunchProjectile, float, IntVec3>>(
            AccessTools.Method(typeof(Verb_LaunchProjectile), "GetForcedMissTarget")
        );

    private static readonly Action<Verb_LaunchProjectile, string> ThrowDebugText =
        AccessTools.MethodDelegate<Action<Verb_LaunchProjectile, string>>(
            AccessTools.Method(typeof(Verb_LaunchProjectile), "ThrowDebugText", [typeof(string)])
        );

    private static readonly Action<Verb_LaunchProjectile, string, IntVec3> ThrowDebugTextAt =
        AccessTools.MethodDelegate<Action<Verb_LaunchProjectile, string, IntVec3>>(
            AccessTools.Method(typeof(Verb_LaunchProjectile), "ThrowDebugText", [typeof(string), typeof(IntVec3)])
        );

    [UsedImplicitly]
    public static bool Prefix(Verb_LaunchProjectile __instance, ref bool __result) {
        using var _ = PerformanceProfiler.Measure("Patch.Verb_LaunchProjectile.TryCastShot");

        var currentTarget = __instance.CurrentTarget;
        var caster = __instance.caster;
        if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map) {
            __result = false;
            return false;
        }

        var projectileDef = __instance.Projectile;
        if (projectileDef == null) {
            __result = false;
            return false;
        }

        var hasLineOfSight = __instance.TryFindShootLineFromTo(caster.Position, currentTarget, out var resultingLine);
        if (__instance.verbProps.stopBurstWithoutLos && !hasLineOfSight) {
            __result = false;
            return false;
        }

        if (__instance.EquipmentSource != null) {
            __instance.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            __instance.EquipmentSource.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
        }

        LastShotTickRef(__instance) = Find.TickManager.TicksGame;

        Thing manningPawn = caster;
        Thing? equipmentSource = __instance.EquipmentSource;
        var compMannable = caster.TryGetComp<CompMannable>();
        if (compMannable?.ManningPawn != null) {
            manningPawn = compMannable.ManningPawn;
            equipmentSource = caster;
        }

        var shootSource = resultingLine.Source;
        var launchOrigin = caster.DrawPos;
        var protectedLeanSupportCell =
            ProjectileCoverUtility.ResolveProtectedLeanSupportCell(caster.Map, caster.Position, shootSource,
                currentTarget);

        // TODO: Keep vanilla forced miss only for explosive projectiles until explosive scatter is refactored.
        if (__instance.verbProps.CausesExplosion && __instance.verbProps.ForcedMissRadius > 0.5f) {
            var forcedMissRadius = __instance.verbProps.ForcedMissRadius;
            if (manningPawn is Pawn pawn && equipmentSource != null) {
                forcedMissRadius *= __instance.verbProps.GetForceMissFactorFor(equipmentSource, pawn);
            }

            var adjustedForcedMissRadius =
                VerbUtility.CalculateAdjustedForcedMiss(forcedMissRadius, currentTarget.Cell - caster.Position);
            if (adjustedForcedMissRadius > 0.5f) {
                var forcedMissTarget = GetForcedMissTarget(__instance, adjustedForcedMissRadius);
                if (forcedMissTarget != currentTarget.Cell) {
                    ThrowDebugText(__instance, "ToRadius");
                    ThrowDebugTextAt(__instance, "Rad\nDest", forcedMissTarget);

                    var projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                    if (Rand.Chance(0.5f)) {
                        projectileHitFlags = ProjectileHitFlags.All;
                    }

                    if (!CanHitNonTargetPawnsNowRef(__instance)) {
                        projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                    }

                    var projectile = SpawnPreparedProjectile();
                    projectile.Launch(manningPawn, launchOrigin, forcedMissTarget, currentTarget, projectileHitFlags,
                        __instance.preventFriendlyFire, equipmentSource);
                    __result = true;
                    return false;
                }
            }
        }

        ThingDef? targetCoverDef = null;
        if (__instance.verbProps.canGoWild) {
            var finalAccuracy = ProjectileAccuracyUtility.GetFinalAccuracy(__instance);
            var spreadDestination =
                ProjectileAccuracyUtility.GetSpreadDestination(resultingLine, caster.Map, finalAccuracy);
            if (spreadDestination != currentTarget.Cell) {
                ThrowDebugText(__instance, "ToSpread" + (CanHitNonTargetPawnsNowRef(__instance) ? "\nchntp" : ""));
                ThrowDebugTextAt(__instance, "Spread\nDest", spreadDestination);

                var projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                if (CanHitNonTargetPawnsNowRef(__instance)) {
                    projectileHitFlags |= ProjectileHitFlags.NonTargetPawns;
                }

                var projectile = SpawnPreparedProjectile();
                projectile.Launch(manningPawn, launchOrigin, spreadDestination, currentTarget, projectileHitFlags,
                    __instance.preventFriendlyFire, equipmentSource, targetCoverDef);
                __result = true;
                return false;
            }
        }

        var directHitFlags = ProjectileHitFlags.IntendedTarget;
        if (CanHitNonTargetPawnsNowRef(__instance)) {
            directHitFlags |= ProjectileHitFlags.NonTargetPawns;
        }

        if (!currentTarget.HasThing || currentTarget.Thing?.def.Fillage == FillCategory.Full) {
            directHitFlags |= ProjectileHitFlags.NonTargetWorld;
        }

        ThrowDebugText(__instance, "ToHit" + (CanHitNonTargetPawnsNowRef(__instance) ? "\nchntp" : ""));
        if (currentTarget.Thing != null) {
            var projectile = SpawnPreparedProjectile();
            var launchDestination = currentTarget.Cell;
            if (currentTarget.Thing is Pawn && resultingLine.Dest != currentTarget.Cell) {
                launchDestination = resultingLine.Dest;
                ProjectileCoverUtility.MarkTargetUsesLeanExposure(projectile);
                ProjectileCoverUtility.OverrideFlightDestination(projectile, launchDestination);
            }

            projectile.Launch(manningPawn, launchOrigin, currentTarget, currentTarget, directHitFlags,
                __instance.preventFriendlyFire, equipmentSource, targetCoverDef);
            ThrowDebugTextAt(__instance, "Hit\nDest", launchDestination);
        } else {
            var projectile = SpawnPreparedProjectile();
            projectile.Launch(manningPawn, launchOrigin, resultingLine.Dest, currentTarget, directHitFlags,
                __instance.preventFriendlyFire, equipmentSource, targetCoverDef);
            ThrowDebugTextAt(__instance, "Hit\nDest", resultingLine.Dest);
        }

        __result = true;
        return false;

        Projectile SpawnPreparedProjectile() {
            using var __ = PerformanceProfiler.Measure("Patch.Verb_LaunchProjectile.SpawnPreparedProjectile");

            var projectile = (Projectile)GenSpawn.Spawn(projectileDef, shootSource, caster.Map);
            ProjectileCoverUtility.OverrideFlightSource(projectile, shootSource);
            ProjectileCoverUtility.OverrideProtectedLeanSupportCell(projectile, protectedLeanSupportCell);

            if (equipmentSource == null || !equipmentSource.TryGetComp<CompUniqueWeapon>(out var comp)) {
                return projectile;
            }

            foreach (var trait in comp.TraitsListForReading) {
                if (trait.damageDefOverride != null) {
                    projectile.damageDefOverride = trait.damageDefOverride;
                }

                if (trait.extraDamages.NullOrEmpty()) {
                    continue;
                }

                projectile.extraDamages ??= [];
                projectile.extraDamages.AddRange(trait.extraDamages);
            }

            return projectile;
        }
    }
}

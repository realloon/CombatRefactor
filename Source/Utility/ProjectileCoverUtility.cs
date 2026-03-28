using HarmonyLib;
using UnityEngine;

namespace CombatRefactor.Utility;

public static class ProjectileCoverUtility {
    private static readonly AccessTools.FieldRef<Projectile, Vector3> DestinationRef =
        AccessTools.FieldRefAccess<Projectile, Vector3>("destination");

    private static readonly AccessTools.FieldRef<Projectile, ThingDef> TargetCoverDefRef =
        AccessTools.FieldRefAccess<Projectile, ThingDef>("targetCoverDef");

    private static readonly AccessTools.FieldRef<Projectile, bool> PreventFriendlyFireRef =
        AccessTools.FieldRefAccess<Projectile, bool>("preventFriendlyFire");

    private static readonly Action<Projectile, Thing, bool> Impact =
        AccessTools.MethodDelegate<Action<Projectile, Thing, bool>>(
            AccessTools.Method(typeof(Projectile), "Impact", [typeof(Thing), typeof(bool)])
        );

    private static readonly Func<Projectile, Thing, bool> CanHit =
        AccessTools.MethodDelegate<Func<Projectile, Thing, bool>>(
            AccessTools.Method(typeof(Projectile), "CanHit", [typeof(Thing)])
        );

    private static readonly Action<Projectile, string, IntVec3> ThrowDebugText =
        AccessTools.MethodDelegate<Action<Projectile, string, IntVec3>>(
            AccessTools.Method(typeof(Projectile), "ThrowDebugText", [typeof(string), typeof(IntVec3)])
        );

    public static void InjectProjectileStageComp() {
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading) {
            if (def.projectile == null) {
                continue;
            }

            def.comps ??= [];
            if (def.comps.OfType<CompProperties_ProjectileStage>().Any()) {
                continue;
            }

            def.comps.Add(new CompProperties_ProjectileStage());
        }
    }

    public static void InitializeForLaunch(Projectile projectile) {
        ApplyPendingFlightDestination(projectile);
        projectile.TryGetComp<CompProjectileStage>()?.RollCoverIntercept();
    }

    public static void OverrideFlightDestination(Projectile projectile, IntVec3 destinationCell) {
        if (!destinationCell.IsValid) {
            return;
        }

        projectile.TryGetComp<CompProjectileStage>()?.SetPendingFlightDestination(destinationCell);
    }

    public static void MarkTargetUsesLeanExposure(Projectile projectile) {
        projectile.TryGetComp<CompProjectileStage>()?.SetUsesTargetLeanExposure(true);
    }

    public static bool HasFriendlyPawnBlocker(Thing launcher, Map map, IntVec3 sourceCell, LocalTargetInfo usedTarget,
        LocalTargetInfo intendedTarget) {
        if (launcher.Faction == null || !sourceCell.InBounds(map) || !usedTarget.IsValid) {
            return false;
        }

        var destinationCell = usedTarget.Cell;
        if (!destinationCell.InBounds(map) || destinationCell == sourceCell) {
            return false;
        }

        if (destinationCell.AdjacentToCardinal(sourceCell)) {
            return CellHasFriendlyPawnBlocker(launcher, map, destinationCell, intendedTarget);
        }

        var lastExactPos = sourceCell.ToVector3Shifted();
        var newExactPos = destinationCell.ToVector3Shifted();
        var cursor = lastExactPos;
        var segment = newExactPos - lastExactPos;
        var step = segment.normalized * 0.2f;
        var maxSteps = (int)(segment.MagnitudeHorizontal() / 0.2f);
        var checkedCells = new HashSet<IntVec3>();

        for (var stepIndex = 0; stepIndex <= maxSteps; stepIndex++) {
            cursor += step;
            var cell = cursor.ToIntVec3();
            if (!checkedCells.Add(cell)) {
                continue;
            }

            if (CellHasFriendlyPawnBlocker(launcher, map, cell, intendedTarget)) {
                return true;
            }

            if (cell == destinationCell) {
                return false;
            }
        }

        return false;
    }

    public static bool TryInterceptCoverBetween(Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos) {
        if (lastExactPos == newExactPos) {
            return false;
        }

        var interceptors = projectile.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
        if (Enumerable.Any(interceptors, t => t.TryGetComp<CompProjectileInterceptor>()
                .CheckIntercept(projectile, lastExactPos, newExactPos))) {
            Impact(projectile, null!, true);
            return true;
        }

        var lastCell = lastExactPos.ToIntVec3();
        var newCell = newExactPos.ToIntVec3();
        if (newCell == lastCell || !lastCell.InBounds(projectile.Map) || !newCell.InBounds(projectile.Map)) {
            return false;
        }

        if (newCell.AdjacentToCardinal(lastCell)) {
            return TryInterceptCoverAtCell(projectile, newCell);
        }

        var cursor = lastExactPos;
        var segment = newExactPos - lastExactPos;
        var step = segment.normalized * 0.2f;
        var maxSteps = (int)(segment.MagnitudeHorizontal() / 0.2f);
        var checkedCells = new HashSet<IntVec3>();

        for (var stepIndex = 0; stepIndex <= maxSteps; stepIndex++) {
            cursor += step;
            var cell = cursor.ToIntVec3();
            if (!checkedCells.Add(cell)) {
                continue;
            }

            if (TryInterceptCoverAtCell(projectile, cell)) {
                return true;
            }

            if (cell == newCell) {
                return false;
            }
        }

        return false;
    }

    public static bool TryHandleLeanTargetImpact(Projectile projectile) {
        var comp = projectile.TryGetComp<CompProjectileStage>();
        if (comp is not { UsesTargetLeanExposure: true }) {
            return false;
        }

        if (!projectile.usedTarget.HasThing || projectile.usedTarget.Thing is not Pawn pawn) {
            return false;
        }

        if (!CanHit(projectile, pawn)) {
            return false;
        }

        var hitChance = Mathf.Clamp01(pawn.BodySize / 2f);
        if (Rand.Chance(hitChance)) {
            return false;
        }

        ThrowDebugText(projectile, $"lean\n{hitChance.ToStringPercent()}", projectile.Position);
        Impact(projectile, null!, false);
        return true;
    }

    private static bool TryInterceptCoverAtCell(Projectile projectile, IntVec3 cell) {
        if (projectile.Map == null || GetTerminalFlightCell(projectile) == cell) {
            return false;
        }

        var coverInterceptRoll = GetCoverInterceptRoll(projectile);
        var thingList = cell.GetThingList(projectile.Map);
        var hasCover = false;

        foreach (var thing in thingList) {
            var coverStrength = GetInterceptStrength(projectile, thing);
            if (coverStrength <= 0f) {
                continue;
            }

            hasCover = true;
            if (coverStrength <= coverInterceptRoll) {
                ThrowDebugText(projectile, coverStrength.ToStringPercent(), cell);
                continue;
            }

            TargetCoverDefRef(projectile) = thing.def;
            ThrowDebugText(projectile, $"cover\n{coverStrength.ToStringPercent()}", cell);
            Impact(projectile, thing, false);
            return true;
        }

        if (!hasCover) {
            ThrowDebugText(projectile, "o", cell);
        }

        return false;
    }

    private static float GetCoverInterceptRoll(Projectile projectile) {
        var comp = projectile.TryGetComp<CompProjectileStage>();
        if (comp == null) {
            return Rand.Value;
        }

        if (!comp.HasCoverInterceptRoll) {
            comp.RollCoverIntercept();
        }

        return comp.CoverInterceptRoll;
    }

    private static void ApplyPendingFlightDestination(Projectile projectile) {
        var comp = projectile.TryGetComp<CompProjectileStage>();
        if (comp == null || !comp.HasPendingFlightDestination) {
            return;
        }

        comp.SetFlightDestination(comp.PendingFlightDestination);
        DestinationRef(projectile) =
            comp.PendingFlightDestination.ToVector3Shifted() + Gen.RandomHorizontalVector(0.3f);
        comp.ClearPendingFlightDestination();
    }

    private static IntVec3 GetTerminalFlightCell(Projectile projectile) {
        var comp = projectile.TryGetComp<CompProjectileStage>();
        return comp is { HasFlightDestination: true }
            ? comp.FlightDestination
            : projectile.usedTarget.Cell;
    }

    private static float GetInterceptStrength(Projectile projectile, Thing thing) {
        if (!thing.Spawned || thing.Map != projectile.Map || thing == projectile.Launcher) {
            return 0f;
        }

        if (thing == projectile.intendedTarget.Thing) {
            return 0f;
        }

        if (thing is Pawn pawn) {
            return GetPawnInterceptStrength(projectile, pawn);
        }

        if (thing is Building_Door { Open: not false }) {
            return 0f;
        }

        if (thing.def.Fillage == FillCategory.Full) {
            return 1f;
        }

        var blockChance = thing.BaseBlockChance();
        if (blockChance <= 0.2f) {
            return 0f;
        }

        return Mathf.Clamp01(blockChance);
    }

    private static bool CellHasFriendlyPawnBlocker(Thing launcher, Map map, IntVec3 cell,
        LocalTargetInfo intendedTarget) {
        if (!cell.InBounds(map)) {
            return false;
        }

        var thingList = cell.GetThingList(map);
        foreach (var t in thingList) {
            if (t is not Pawn pawn) continue;

            if (pawn == launcher || pawn == intendedTarget.Thing) continue;

            if (pawn.Faction == null || launcher.Faction == null || pawn.Faction.HostileTo(launcher.Faction)) {
                continue;
            }

            return true;
        }

        return false;
    }

    private static float GetPawnInterceptStrength(Projectile projectile, Pawn pawn) {
        if ((projectile.HitFlags & ProjectileHitFlags.NonTargetPawns) == ProjectileHitFlags.None) {
            return 0f;
        }

        var interceptStrength = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
        if (pawn.GetPosture() != PawnPosture.Standing) {
            interceptStrength *= 0.1f;
        }

        if (projectile.Launcher != null &&
            pawn.Faction != null &&
            projectile.Launcher.Faction != null &&
            !pawn.Faction.HostileTo(projectile.Launcher.Faction)) {
            interceptStrength *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
        }

        if (interceptStrength <= 0f) {
            return 0f;
        }

        if (projectile.Launcher != null &&
            pawn.Faction != null &&
            projectile.Launcher.Faction != null &&
            !pawn.Faction.HostileTo(projectile.Launcher.Faction) &&
            PreventFriendlyFireRef(projectile)) {
            return 0f;
        }

        return interceptStrength;
    }
}
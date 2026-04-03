using System.Text;
using HarmonyLib;
using UnityEngine;

namespace CombatRefactor.Utility;

public static class ProjectileReadoutUtility {
    private static readonly AccessTools.FieldRef<Verb, bool> CanHitNonTargetPawnsNowRef =
        AccessTools.FieldRefAccess<Verb, bool>("canHitNonTargetPawnsNow");

    public static bool TryBuildShotCalculationTipString(Thing target, out string readout) {
        readout = string.Empty;

        if (Find.Selector.SingleSelectedThing == null) {
            return false;
        }

        var selectedThing = Find.Selector.SingleSelectedThing;
        var verb = ResolveVerb(selectedThing, target);
        if (verb == null) {
            return false;
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.Append("ShotBy".Translate(selectedThing.LabelShort, selectedThing));
        stringBuilder.Append(": ");

        if (!verb.CanHitTarget(target)) {
            stringBuilder.AppendLine("CannotHit".Translate());
            readout = stringBuilder.ToString();
            return true;
        }

        stringBuilder.Append(BuildLaunchProjectileReadout(verb, new LocalTargetInfo(target)));
        AppendManhunterOnDamageChance(stringBuilder, target, selectedThing, verb);
        readout = stringBuilder.ToString();
        return true;
    }

    private static Verb_LaunchProjectile? ResolveVerb(Thing selectedThing, Thing target) {
        if (selectedThing is Pawn pawn &&
            pawn != target &&
            pawn.equipment?.Primary != null &&
            pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectile verb &&
            (!pawn.IsPlayerControlled || pawn.Drafted)) {
            return verb;
        }

        return selectedThing is Building_TurretGun buildingTurretGun && buildingTurretGun != target
            ? buildingTurretGun.AttackVerb as Verb_LaunchProjectile
            : null;
    }

    private static string BuildLaunchProjectileReadout(Verb_LaunchProjectile verb, LocalTargetInfo target) {
        var stringBuilder = new StringBuilder();

        if (verb.verbProps.ForcedMissRadius > 0.5f) {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("WeaponMissRadius".Translate() + ": " +
                                     verb.verbProps.ForcedMissRadius.ToString("F1"));
        }

        if (!verb.TryFindShootLineFromTo(verb.caster.Position, target, out var shootLine)) {
            stringBuilder.AppendLine("CannotHit".Translate());
            return stringBuilder.ToString();
        }

        var stageOne = BuildStageOneReadout(verb);
        var stageTwo = BuildStageTwoReadout(verb, target, shootLine);

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("CRTeam_Readout_ShootingAccuracy".Translate(stageOne.FinalAccuracy.ToStringPercent()));
        stringBuilder.AppendLine("CRTeam_Readout_WeaponAccuracy".Translate(stageOne.WeaponAccuracy.ToStringPercent()));
        stringBuilder.AppendLine("CRTeam_Readout_ShooterFactor".Translate(stageOne.ShooterAccuracy.ToStringPercent()));
        stringBuilder.AppendLine("CRTeam_Readout_BurstFactor".Translate(stageOne.BurstFactor.ToStringPercent()));

        stringBuilder.AppendLine("CRTeam_Readout_PathClearance".Translate(stageTwo.PassChance.ToStringPercent()));
        if (stageTwo.StrongestBlocker != null) {
            stringBuilder.AppendLine("CRTeam_Readout_StrongestBlocker"
                .Translate(stageTwo.StrongestBlocker.LabelCap, stageTwo.StrongestIntercept.ToStringPercent()));
        }

        if (target.Thing is Pawn pawn) {
            var usesLeanExposure = UsesLeanExposure(target, shootLine);
            var bodySizeFactor = pawn.BodySize;
            var postureFactor = ProjectileCoverUtility.GetDirectTargetPostureFactor(pawn);
            var leanFactor = ProjectileCoverUtility.GetDirectTargetLeanFactor(usesLeanExposure);
            var hitChance = ProjectileCoverUtility.GetDirectTargetHitChance(pawn, usesLeanExposure);

            stringBuilder.AppendLine("CRTeam_Readout_TargetExposure".Translate(hitChance.ToStringPercent()));
            stringBuilder.AppendLine("CRTeam_Readout_BodySize".Translate(bodySizeFactor.ToStringPercent()));
            stringBuilder.AppendLine("CRTeam_Readout_Posture".Translate(postureFactor.ToStringPercent()));
            stringBuilder.AppendLine("CRTeam_Readout_LeanExposure".Translate(leanFactor.ToStringPercent()));
        }

        return stringBuilder.ToString();
    }

    private static StageOnePreview BuildStageOneReadout(Verb_LaunchProjectile verb) {
        var weaponAccuracy = ProjectileAccuracyUtility.GetWeaponAccuracy(verb);
        var shooterAccuracy = ProjectileAccuracyUtility.GetShooterAccuracy(verb.caster);
        var burstFactor = ProjectileAccuracyUtility.GetBurstShotAccuracyFactor(verb);
        var finalAccuracy = ProjectileAccuracyUtility.GetFinalAccuracy(verb);

        return new StageOnePreview {
            WeaponAccuracy = weaponAccuracy,
            ShooterAccuracy = shooterAccuracy,
            BurstFactor = burstFactor,
            FinalAccuracy = finalAccuracy
        };
    }

    private static StageTwoPreview BuildStageTwoReadout(Verb_LaunchProjectile verb, LocalTargetInfo target,
        ShootLine shootLine) {
        var sourceCell = shootLine.Source;
        var terminalCell = UsesLeanExposure(target, shootLine) ? shootLine.Dest : target.Cell;
        var protectedLeanSupportCell =
            ProjectileCoverUtility.ResolveProtectedLeanSupportCell(verb.caster.Map, verb.caster.Position, sourceCell,
                target);

        var strongestBlocker = default(Thing);
        var strongestIntercept = 0f;

        foreach (var cell in GenSight.PointsOnLineOfSight(sourceCell, terminalCell)) {
            if (cell == sourceCell || cell == terminalCell) {
                continue;
            }

            foreach (var thing in cell.GetThingList(verb.caster.Map)) {
                if (IsProtectedLeanSupportThing(verb.caster.Map, protectedLeanSupportCell, cell, thing)) {
                    continue;
                }

                var interceptStrength = GetPreviewInterceptStrength(verb, target, thing);
                if (interceptStrength <= strongestIntercept) {
                    continue;
                }

                strongestIntercept = interceptStrength;
                strongestBlocker = thing;
            }
        }

        return new StageTwoPreview {
            PassChance = 1f - strongestIntercept,
            StrongestBlocker = strongestBlocker,
            StrongestIntercept = strongestIntercept
        };
    }

    private static bool UsesLeanExposure(LocalTargetInfo target, ShootLine shootLine) {
        return target.Thing is Pawn && shootLine.Dest != target.Cell;
    }

    private static bool IsProtectedLeanSupportThing(Map map, IntVec3 protectedLeanSupportCell, IntVec3 cell,
        Thing thing) {
        return protectedLeanSupportCell.IsValid &&
               protectedLeanSupportCell == cell &&
               cell.GetCover(map) == thing;
    }

    private static float GetPreviewInterceptStrength(Verb_LaunchProjectile verb, LocalTargetInfo intendedTarget,
        Thing thing) {
        if (!thing.Spawned || thing.Map != verb.caster.Map || thing == verb.caster || thing == intendedTarget.Thing) {
            return 0f;
        }

        switch (thing) {
            case Pawn pawn:
                return GetPreviewPawnInterceptStrength(verb, pawn);
            case Building_Door { Open: not false }:
                return 0f;
        }

        if (thing.def.Fillage == FillCategory.Full) {
            return 1f;
        }

        var blockChance = thing.BaseBlockChance();
        return blockChance <= 0.2f ? 0f : Mathf.Clamp01(blockChance);
    }

    private static float GetPreviewPawnInterceptStrength(Verb_LaunchProjectile verb, Pawn pawn) {
        if (!CanHitNonTargetPawnsNowRef(verb)) {
            return 0f;
        }

        var interceptStrength = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
        if (pawn.GetPosture() != PawnPosture.Standing) {
            interceptStrength *= 0.1f;
        }

        if (verb.caster.Faction != null &&
            pawn.Faction != null &&
            !pawn.Faction.HostileTo(verb.caster.Faction)) {
            if (verb.preventFriendlyFire) {
                return 0f;
            }

            interceptStrength *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
        }

        return interceptStrength;
    }

    private static void AppendManhunterOnDamageChance(StringBuilder stringBuilder, Thing target, Thing selectedThing,
        Verb_LaunchProjectile verb) {
        if (target is not Pawn { Faction: null, InAggroMentalState: false } pawn || !pawn.AnimalOrWildMan()) {
            return;
        }

        var manhunterChance = PawnUtility.GetManhunterOnDamageChance(pawn, selectedThing);
        if (verb.IsMeleeAttack) {
            manhunterChance = PawnUtility.GetManhunterOnDamageChance(pawn, selectedThing, 0f);
        }

        if (manhunterChance <= 0f) {
            return;
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("ManhunterPerHit".Translate() + ": " + manhunterChance.ToStringPercent());
    }

    private struct StageOnePreview {
        public float WeaponAccuracy;
        public float ShooterAccuracy;
        public float BurstFactor;
        public float FinalAccuracy;
    }

    private struct StageTwoPreview {
        public float PassChance;
        public Thing? StrongestBlocker;
        public float StrongestIntercept;
    }
}

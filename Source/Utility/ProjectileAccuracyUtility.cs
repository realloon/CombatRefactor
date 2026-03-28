using UnityEngine;

namespace CombatRefactor.Utility;

public static class ProjectileAccuracyUtility {
    private const float MaximumSpreadAngleDegrees = 45f;
    private const float SpreadCurveExponent = 2f;
    private const float AdditionalBurstShotAccuracyPenalty = 0.1f;

    private static readonly SimpleCurve ShooterAccuracyFactorCurve = [
        new CurvePoint(0.70f, 0.15f),
        new CurvePoint(0.80f, 0.28f),
        new CurvePoint(0.89f, 0.42f),
        new CurvePoint(0.91f, 0.48f),
        new CurvePoint(0.935f, 0.58f),
        new CurvePoint(0.965f, 0.74f),
        new CurvePoint(0.9817f, 0.86f),
        new CurvePoint(0.9867f, 0.94f),
        new CurvePoint(0.9883f, 0.97f),
        new CurvePoint(0.99f, 1.00f),
        new CurvePoint(1.00f, 1.00f)
    ];

    public static float GetWeaponAccuracy(Verb_LaunchProjectile verb) {
        var equipment = verb.EquipmentSource;
        if (equipment != null) {
            return Mathf.Max(
                equipment.GetStatValue(StatDefOf.AccuracyTouch),
                equipment.GetStatValue(StatDefOf.AccuracyShort),
                equipment.GetStatValue(StatDefOf.AccuracyMedium),
                equipment.GetStatValue(StatDefOf.AccuracyLong)
            );
        }

        return Mathf.Max(
            verb.verbProps.accuracyTouch,
            verb.verbProps.accuracyShort,
            verb.verbProps.accuracyMedium,
            verb.verbProps.accuracyLong
        );
    }

    public static float GetShooterAccuracy(Thing caster) {
        var sourceAccuracy = caster is Pawn pawn
            ? pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn)
            : caster.GetStatValue(StatDefOf.ShootingAccuracyTurret);

        return Mathf.Clamp01(ShooterAccuracyFactorCurve.Evaluate(Mathf.Clamp(sourceAccuracy, 0f, 1f)));
    }

    public static float GetShooterSourceAccuracy(Thing caster) {
        if (caster is Pawn pawn) {
            return Mathf.Clamp01(pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn));
        }

        return Mathf.Clamp01(caster.GetStatValue(StatDefOf.ShootingAccuracyTurret));
    }

    public static float GetBurstShotAccuracyFactor(Verb_LaunchProjectile verb) {
        var burstShotCount = Mathf.Max(1, verb.BurstShotCount);
        return Mathf.Clamp01(1f - (burstShotCount - 1) * AdditionalBurstShotAccuracyPenalty);
    }

    public static float GetFinalAccuracy(Verb_LaunchProjectile verb) {
        using var _ = PerformanceProfiler.Measure("Accuracy.GetFinalAccuracy");

        return CalculateFinalAccuracy(verb);
    }

    public static float GetMaximumSpreadAngle(float finalAccuracy) {
        return MaximumSpreadAngleDegrees * Mathf.Pow(1f - Mathf.Clamp01(finalAccuracy), SpreadCurveExponent);
    }

    public static IntVec3 GetSpreadDestination(ShootLine shootLine, Map map, float finalAccuracy) {
        var maximumSpreadAngle = GetMaximumSpreadAngle(finalAccuracy);
        if (maximumSpreadAngle <= 0f) {
            return shootLine.Dest;
        }

        var source = shootLine.Source.ToVector3Shifted();
        var destination = shootLine.Dest.ToVector3Shifted();
        var shotVector = (destination - source).Yto0();
        if (shotVector.sqrMagnitude <= 0.0001f) {
            return shootLine.Dest;
        }

        var spreadAngle = Rand.Range(-maximumSpreadAngle, maximumSpreadAngle);

        return Mathf.Abs(spreadAngle) <= 0.0001f
            ? shootLine.Dest
            : (source + shotVector.RotatedBy(spreadAngle)).ToIntVec3().ClampInsideMap(map);
    }

    private static float CalculateFinalAccuracy(Verb_LaunchProjectile verb) {
        return Mathf.Clamp01(GetWeaponAccuracy(verb) *
                             GetShooterAccuracy(verb.caster) *
                             GetBurstShotAccuracyFactor(verb));
    }
}

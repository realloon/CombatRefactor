using UnityEngine;

namespace CombatRefactor;

public static class ProjectileAccuracyUtility {
    private const float MaximumSpreadAngleDegrees = 15f;
    private const float SpreadCurveExponent = 2f;

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
        if (caster is Pawn pawn) {
            return Mathf.Clamp01(pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn));
        }

        return Mathf.Clamp01(caster?.GetStatValue(StatDefOf.ShootingAccuracyTurret) ?? 1f);
    }

    public static float GetFinalAccuracy(Verb_LaunchProjectile verb) {
        return Mathf.Clamp01(GetWeaponAccuracy(verb) * GetShooterAccuracy(verb.caster));
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
        if (Mathf.Abs(spreadAngle) <= 0.0001f) {
            return shootLine.Dest;
        }

        return (source + shotVector.RotatedBy(spreadAngle)).ToIntVec3().ClampInsideMap(map);
    }
}

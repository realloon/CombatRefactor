using UnityEngine;

namespace CombatRefactor;

public static class ProjectileSpreadVisualizationUtility {
    private const float DashLength = 0.7f;
    private const float GapLength = 0.35f;
    private const float LineWidth = 0.08f;

    public static void DrawSpreadBounds(Verb_LaunchProjectile verb, LocalTargetInfo target) {
        if (verb.Caster == null || !target.IsValid || verb.Caster.Map == null) {
            return;
        }

        var source = verb.Caster.DrawPos;
        var targetVector = (target.CenterVector3 - source).Yto0();
        if (targetVector.sqrMagnitude <= 0.0001f) {
            return;
        }

        var maximumSpreadAngle = ProjectileAccuracyUtility.GetMaximumSpreadAngle(ProjectileAccuracyUtility.GetFinalAccuracy(verb));
        if (maximumSpreadAngle <= 0f) {
            return;
        }

        DrawDashedRay(source, targetVector.RotatedBy(-maximumSpreadAngle), SimpleColor.White);
        DrawDashedRay(source, targetVector.RotatedBy(maximumSpreadAngle), SimpleColor.White);
    }

    private static void DrawDashedRay(Vector3 source, Vector3 ray, SimpleColor color) {
        var rayLength = ray.MagnitudeHorizontal();
        if (rayLength <= 0.0001f) {
            return;
        }

        var direction = ray / rayLength;
        var distance = 0f;
        while (distance < rayLength) {
            var dashStart = source + direction * distance;
            var dashEnd = source + direction * Mathf.Min(distance + DashLength, rayLength);
            GenDraw.DrawLineBetween(dashStart, dashEnd, color, LineWidth);
            distance += DashLength + GapLength;
        }
    }
}

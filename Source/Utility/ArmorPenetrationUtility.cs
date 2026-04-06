using UnityEngine;

namespace CombatRefactor.Utility;

public static class ArmorPenetrationUtility {
    private const float ScratchThreshold = 0.15f;
    private const float HardBlockThreshold = 0.35f;
    private const float InheritedArmorFactor = 0.5f;
    private const float MinArmorStat = 0.001f;
    private const float BluntDurabilityScale = 0.35f;
    private const float SharpAbsorbDurabilityScale = 0.25f;
    private const float SharpPenetrationDurabilityScale = 0.12f;
    private static readonly BodyPartGroupDef? ArmsGroup = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Arms");
    private static readonly BodyPartGroupDef? HandsGroup = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Hands");
    private static readonly BodyPartGroupDef? FeetGroup = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Feet");

    public static bool ShouldHandle(DamageDef damageDef) {
        return damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp ||
               IsBluntDamage(damageDef);
    }

    public static float GetPostArmorDamage(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part,
        ref DamageDef damageDef, out bool deflectedByMetalArmor, out bool diminishedByMetalArmor) {
        deflectedByMetalArmor = false;
        diminishedByMetalArmor = false;

        if (amount <= 0f) return amount;

        if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp) {
            return ResolveSharpDamage(pawn, amount, armorPenetration, part, out deflectedByMetalArmor,
                out diminishedByMetalArmor);
        }

        if (IsBluntDamage(damageDef)) {
            return ResolveBluntDamage(pawn, amount, armorPenetration, part, out deflectedByMetalArmor,
                out diminishedByMetalArmor);
        }

        return amount;
    }

    private static float ResolveSharpDamage(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part,
        out bool deflectedByMetalArmor, out bool diminishedByMetalArmor) {
        deflectedByMetalArmor = false;
        diminishedByMetalArmor = false;

        var remainingDamage = amount;
        foreach (var layer in EnumerateArmorLayers(pawn, part)) {
            var previousDamage = remainingDamage;
            var result = ResolveSharpLayer(previousDamage, armorPenetration, layer.ArmorRating);
            remainingDamage = result.RemainingDamage;
            var durabilityDamage = CalculateSharpDurabilityDamage(previousDamage, result, layer.BluntArmorRating);

            if (result.BlockedDamage > 0.001f) diminishedByMetalArmor |= layer.IsMetal;

            if (layer.Apparel != null) {
                ApplyDurabilityDamage(layer.Apparel, DamageDefOf.Blunt, durabilityDamage);
            }

            if (!(remainingDamage <= 0.001f)) continue;

            deflectedByMetalArmor = layer.IsMetal;

            return 0f;
        }

        return Mathf.Max(remainingDamage, 0f);
    }

    private static float ResolveBluntDamage(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part,
        out bool deflectedByMetalArmor, out bool diminishedByMetalArmor) {
        deflectedByMetalArmor = false;
        diminishedByMetalArmor = false;

        var remainingDamage = amount;
        foreach (var layer in EnumerateArmorLayers(pawn, part)) {
            var previousDamage = remainingDamage;
            var result = ResolveBluntLayer(previousDamage, armorPenetration, layer.BluntArmorRating);
            remainingDamage = result.RemainingDamage;

            if (result.BlockedDamage > 0.001f) diminishedByMetalArmor |= layer.IsMetal;

            if (layer.Apparel != null) {
                var durabilityDamage = CalculateBlockedDurabilityDamage(result.BlockedDamage, layer.BluntArmorRating,
                    BluntDurabilityScale);
                ApplyDurabilityDamage(layer.Apparel, DamageDefOf.Blunt, durabilityDamage);
            }

            if (!(remainingDamage <= 0.001f)) continue;

            deflectedByMetalArmor = layer.IsMetal;

            return 0f;
        }

        return Mathf.Max(remainingDamage, 0f);
    }

    private static IEnumerable<ArmorLayer> EnumerateArmorLayers(Pawn pawn, BodyPartRecord part) {
        if (pawn.apparel != null) {
            var wornApparel = pawn.apparel.WornApparel;
            for (var i = wornApparel.Count - 1; i >= 0; i--) {
                var apparel = wornApparel[i];
                if (apparel.def.apparel.CoversBodyPart(part)) {
                    yield return new ArmorLayer(
                        apparel,
                        apparel.GetStatValue(StatDefOf.ArmorRating_Sharp),
                        apparel.GetStatValue(StatDefOf.ArmorRating_Blunt),
                        IsMetalArmor(apparel)
                    );
                    continue;
                }

                if (!TryGetInheritedCoverageSource(apparel, part)) continue;

                yield return new ArmorLayer(
                    apparel,
                    apparel.GetStatValue(StatDefOf.ArmorRating_Sharp) * InheritedArmorFactor,
                    apparel.GetStatValue(StatDefOf.ArmorRating_Blunt) * InheritedArmorFactor,
                    IsMetalArmor(apparel)
                );
            }
        }

        yield return new ArmorLayer(
            null,
            pawn.GetStatValue(StatDefOf.ArmorRating_Sharp),
            pawn.GetStatValue(StatDefOf.ArmorRating_Blunt),
            pawn.RaceProps.IsMechanoid
        );
    }

    private static SharpLayerResult ResolveSharpLayer(float damage, float armorPenetration, float armorRating) {
        if (damage <= 0f || armorRating <= MinArmorStat) {
            return new SharpLayerResult(damage, 0f, 1f, SharpResolutionStage.FullPenetration);
        }

        var penetrationRatio = armorPenetration / armorRating;
        return penetrationRatio switch {
            < ScratchThreshold => new SharpLayerResult(0f, damage, 0f, SharpResolutionStage.Scratch),
            < HardBlockThreshold => new SharpLayerResult(0f, damage, 0f, SharpResolutionStage.HardBlock),
            >= 1f => new SharpLayerResult(damage, 0f, 1f, SharpResolutionStage.FullPenetration),
            _ => CreatePartialSharpLayerResult(damage, penetrationRatio)
        };
    }

    private static LayerResolutionResult ResolveBluntLayer(float damage, float armorPenetration, float armorRating) {
        if (damage <= 0f || armorRating <= MinArmorStat) {
            return new LayerResolutionResult(damage, 0f);
        }

        var penetrationRatio = armorPenetration / armorRating;
        return penetrationRatio switch {
            < ScratchThreshold => new LayerResolutionResult(0f, damage),
            >= 1f => new LayerResolutionResult(damage, 0f),
            _ => new LayerResolutionResult(damage * penetrationRatio, damage - damage * penetrationRatio)
        };
    }

    private static SharpLayerResult CreatePartialSharpLayerResult(float damage, float penetrationRatio) {
        var passThroughRatio = (penetrationRatio - HardBlockThreshold) / (1f - HardBlockThreshold);
        passThroughRatio = Mathf.Clamp01(passThroughRatio);
        var remainingDamage = damage * passThroughRatio;
        return new SharpLayerResult(remainingDamage, damage - remainingDamage, passThroughRatio,
            SharpResolutionStage.PartialPenetration);
    }

    private static float CalculateSharpDurabilityDamage(float sourceDamage, SharpLayerResult result, float bluntArmor) {
        switch (result.Stage) {
            case SharpResolutionStage.Scratch:
                return 0f;
            case SharpResolutionStage.HardBlock:
                return CalculateBlockedDurabilityDamage(result.BlockedDamage, bluntArmor, SharpAbsorbDurabilityScale);
            case SharpResolutionStage.PartialPenetration:
                var absorbedDamage = CalculateBlockedDurabilityDamage(result.BlockedDamage, bluntArmor,
                    SharpAbsorbDurabilityScale);
                var penetrationDamage = sourceDamage * result.PenetrationRatio * SharpPenetrationDurabilityScale /
                                        GetDurabilityResistance(bluntArmor);
                return absorbedDamage + penetrationDamage;
            case SharpResolutionStage.FullPenetration:
                return sourceDamage * SharpPenetrationDurabilityScale / GetDurabilityResistance(bluntArmor);
            default:
                return 0f;
        }
    }

    private static float CalculateBlockedDurabilityDamage(float blockedDamage, float bluntArmor, float scale) {
        if (blockedDamage <= 0.001f) return 0f;

        return blockedDamage * scale / GetDurabilityResistance(bluntArmor);
    }

    private static float GetDurabilityResistance(float bluntArmor) {
        return Mathf.Max(1f + Mathf.Max(bluntArmor, 0f), 0.1f);
    }

    private static void ApplyDurabilityDamage(Apparel apparel, DamageDef damageDef, float damageAmount) {
        if (damageAmount <= 0.001f) return;

        apparel.TakeDamage(new DamageInfo(damageDef, damageAmount));
    }

    private static bool IsMetalArmor(Apparel apparel) {
        return apparel.def.apparel.useDeflectMetalEffect || (apparel.Stuff?.IsMetal ?? false);
    }

    private static bool TryGetInheritedCoverageSource(Apparel apparel, BodyPartRecord part) {
        if (!IsEligibleForInheritedArmor(apparel)) return false;

        if (IsEyePart(part) &&
            (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead) ||
             apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead))) {
            return true;
        }

        if (IsHandExtremity(part) && ArmsGroup != null && apparel.def.apparel.bodyPartGroups.Contains(ArmsGroup)) {
            return true;
        }

        return IsFootExtremity(part) && apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs);
    }

    private static bool IsEligibleForInheritedArmor(Apparel apparel) {
        return apparel.def.thingCategories != null &&
               (apparel.def.thingCategories.Contains(ThingCategoryDefOf.ApparelArmor)
                || apparel.def.thingCategories.Contains(ThingCategoryDefOf.ArmorHeadgear));
    }

    private static bool IsEyePart(BodyPartRecord part) {
        return part.IsInGroup(BodyPartGroupDefOf.Eyes) || part.def.defName == "Eye";
    }

    private static bool IsHandExtremity(BodyPartRecord part) {
        return part.def.defName is "Hand" or "Finger" ||
               part.IsInGroup(BodyPartGroupDefOf.LeftHand) ||
               part.IsInGroup(BodyPartGroupDefOf.RightHand) ||
               HandsGroup != null && part.IsInGroup(HandsGroup);
    }

    private static bool IsFootExtremity(BodyPartRecord part) {
        return part.def.defName is "Foot" or "Toe" ||
               FeetGroup != null && part.IsInGroup(FeetGroup);
    }

    private static bool IsBluntDamage(DamageDef damageDef) {
        return damageDef == DamageDefOf.Blunt
               || damageDef.workerClass != null
               && typeof(DamageWorker_Blunt).IsAssignableFrom(damageDef.workerClass);
    }

    private readonly struct ArmorLayer(Apparel? apparel, float armorRating, float bluntArmorRating, bool isMetal) {
        public Apparel? Apparel { get; } = apparel;
        public float ArmorRating { get; } = armorRating;
        public float BluntArmorRating { get; } = bluntArmorRating;
        public bool IsMetal { get; } = isMetal;
    }

    private readonly struct LayerResolutionResult(float remainingDamage, float blockedDamage) {
        public float RemainingDamage { get; } = remainingDamage;
        public float BlockedDamage { get; } = blockedDamage;
    }

    private readonly struct SharpLayerResult(
        float remainingDamage,
        float blockedDamage,
        float penetrationRatio,
        SharpResolutionStage stage) {
        public float RemainingDamage { get; } = remainingDamage;
        public float BlockedDamage { get; } = blockedDamage;
        public float PenetrationRatio { get; } = penetrationRatio;
        public SharpResolutionStage Stage { get; } = stage;
    }

    private enum SharpResolutionStage {
        Scratch,
        HardBlock,
        PartialPenetration,
        FullPenetration
    }
}
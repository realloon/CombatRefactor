using UnityEngine;

namespace CombatRefactor;

public class CompFireSelector : ThingComp, IEquippedGizmoProvider {
    private FireMode _currentMode;

    private CompProperties_FireSelector Props => (CompProperties_FireSelector)props;

    public override void Initialize(CompProperties properties) {
        base.Initialize(properties);
        _currentMode = Props.defaultMode;
    }

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Values.Look(ref _currentMode, "currentMode", Props.defaultMode);
    }

    public int GetBurstShotCountFor(int originalBurstShotCount) {
        var burstShotCount = ResolveBurstShotCount(originalBurstShotCount);

        return _currentMode switch {
            FireMode.Single => 1,
            FireMode.Burst => burstShotCount,
            FireMode.Auto => ResolveAutoBurstShotCount(burstShotCount),
            _ => burstShotCount,
        };
    }

    public IEnumerable<Gizmo> GetEquippedGizmos() {
        var pawn = GetEquippingPawn();
        if (pawn == null || !CanShowFor(pawn)) {
            yield break;
        }

        if (!PawnAttackGizmoUtility.CanShowEquipmentGizmos()) {
            yield break;
        }

        yield return new Command_Action {
            defaultLabel = GetModeLabel(_currentMode),
            defaultDesc = "Switch shot mode",
            icon = TexCommand.Attack,
            activateSound = SoundDefOf.Click,
            action = CycleMode,
        };
    }

    private void CycleMode() {
        _currentMode = GetNextMode(_currentMode);
    }

    private FireMode GetNextMode(FireMode mode) {
        if (!HasBurstMode()) {
            return mode == FireMode.Single ? FireMode.Auto : FireMode.Single;
        }

        return mode switch {
            FireMode.Single => FireMode.Burst,
            FireMode.Burst => FireMode.Auto,
            _ => FireMode.Single,
        };
    }

    private int ResolveBurstShotCount(int originalBurstShotCount) {
        return Props.burstShotCountOverride > 0
            ? Props.burstShotCountOverride
            : originalBurstShotCount;
    }

    private bool HasBurstMode() {
        if (Props.burstShotCountOverride > 0) {
            return Props.burstShotCountOverride > 1;
        }

        return parent.def.Verbs != null && Enumerable.Any(parent.def.Verbs, verb => verb.burstShotCount > 1);
    }

    private int ResolveAutoBurstShotCount(int burstShotCount) {
        var multiplier = Mathf.Max(1f, Props.autoBurstMultiplier);
        var autoBurstShotCount = Mathf.Max(burstShotCount, Mathf.CeilToInt(burstShotCount * multiplier));

        if (Props.autoBurstShotCountCap > 0) {
            autoBurstShotCount = Mathf.Min(autoBurstShotCount, Props.autoBurstShotCountCap);
        }

        return autoBurstShotCount;
    }

    private Pawn? GetEquippingPawn() {
        return parent.ParentHolder is Pawn_EquipmentTracker equipmentTracker ? equipmentTracker.pawn : null;
    }

    private static bool CanShowFor(Pawn pawn) {
        return pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled ||
               pawn.IsColonySubhumanPlayerControlled;
    }

    private static string GetModeLabel(FireMode mode) => mode switch {
        FireMode.Single => "Single",
        FireMode.Burst => "Burst",
        FireMode.Auto => "Auto",
        _ => mode.ToString(),
    };
}
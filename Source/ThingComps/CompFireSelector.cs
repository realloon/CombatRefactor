namespace CombatRefactor;

public class CompFireSelector : ThingComp, IEquippedGizmoProvider {
    private FireMode _currentMode;

    private CompProperties_FireSelector Props => (CompProperties_FireSelector)props;

    public override void Initialize(CompProperties properties) {
        base.Initialize(properties);
        _currentMode = GetDefaultMode();
    }

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Values.Look(ref _currentMode, "currentMode", GetDefaultMode());
    }

    public int GetBurstShotCountFor(int originalBurstShotCount) {
        return _currentMode switch {
            FireMode.Single => 1,
            FireMode.Burst => originalBurstShotCount,
            FireMode.Auto => ResolveAutoBurstShotCount(originalBurstShotCount),
            _ => originalBurstShotCount,
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

    private FireMode GetDefaultMode() {
        return HasBurstMode() ? FireMode.Burst : FireMode.Single;
    }

    private bool HasBurstMode() {
        return parent.def.Verbs != null && Enumerable.Any(parent.def.Verbs, verb => verb.burstShotCount > 1);
    }

    private int ResolveAutoBurstShotCount(int burstShotCount) {
        return Props.autoBurstShotCount > 0
            ? Props.autoBurstShotCount
            : burstShotCount * 2;
    }

    private Pawn? GetEquippingPawn() {
        return parent.ParentHolder is Pawn_EquipmentTracker equipmentTracker ? equipmentTracker.pawn : null;
    }

    private static bool CanShowFor(Pawn pawn) {
        return pawn.drafter is { Drafted: true } &&
               (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled ||
                pawn.IsColonySubhumanPlayerControlled);
    }

    private static string GetModeLabel(FireMode mode) => mode switch {
        FireMode.Single => "Single",
        FireMode.Burst => "Burst",
        FireMode.Auto => "Auto",
        _ => mode.ToString(),
    };
}

using Verse.AI;

namespace CombatRefactor;

public class CompFireSelector : ThingComp, IEquippedGizmoProvider {
    private const int SwitchFireModeTicks = 15;

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

        var switchFireModeCommand = new Command_Action {
            defaultLabel = GetModeLabel(_currentMode),
            defaultDesc = "Switch shot mode",
            icon = TexCommand.Attack,
            activateSound = SoundDefOf.Click,
            action = () => TryStartSwitchFireModeJob(pawn),
        };

        if (!CanSwitchFireMode(pawn, out var disabledReason)) {
            switchFireModeCommand.Disable(disabledReason);
        }

        yield return switchFireModeCommand;
    }

    public bool IsHeldBy(Pawn pawn) {
        return pawn.equipment?.Primary == parent && GetEquippingPawn() == pawn;
    }

    private bool IsSwitchingFireMode(Pawn pawn) {
        return pawn.CurJob?.def == JobDefOf.CRTeam_SwitchFireMode &&
               pawn.CurJob.targetB.Thing == parent;
    }

    private bool CanSwitchFireMode(Pawn pawn, out string disabledReason) {
        if (!IsHeldBy(pawn)) {
            disabledReason = "未装备当前武器";
            return false;
        }

        if (IsSwitchingFireMode(pawn)) {
            disabledReason = "正在调整快慢机";
            return false;
        }

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) {
            disabledReason = "无法操作武器";
            return false;
        }

        disabledReason = string.Empty;
        return true;
    }

    public int GetSwitchFireModeTicks() {
        return SwitchFireModeTicks;
    }

    public void CompleteSwitchFireMode() {
        _currentMode = GetNextMode(_currentMode);
    }

    private bool TryStartSwitchFireModeJob(Pawn pawn, bool showFailureMessage = true) {
        if (!CanSwitchFireMode(pawn, out var disabledReason)) {
            if (showFailureMessage && !disabledReason.NullOrEmpty()) {
                Messages.Message(disabledReason, pawn, MessageTypeDefOf.RejectInput, historical: false);
            }

            return false;
        }

        var switchFireModeJob = JobMaker.MakeJob(JobDefOf.CRTeam_SwitchFireMode, pawn, parent);
        pawn.jobs.StartJob(switchFireModeJob, JobCondition.InterruptForced, resumeCurJobAfterwards: true,
            tag: JobTag.Misc);
        return true;
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

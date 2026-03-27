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
            action = () => ShowSwitchFireModeMenu(pawn),
        };

        if (!CanOpenSwitchFireModeMenu(pawn, out var disabledReason)) {
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

    private bool CanOpenSwitchFireModeMenu(Pawn pawn, out string disabledReason) {
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

    public FireMode CurrentMode => _currentMode;

    public int GetSwitchFireModeTicks() {
        return SwitchFireModeTicks;
    }

    public void CompleteSwitchFireMode(FireMode targetMode) {
        _currentMode = targetMode;
    }

    private bool TryStartSwitchFireModeJob(Pawn pawn, FireMode targetMode, bool showFailureMessage = true) {
        if (!CanSwitchToFireMode(pawn, targetMode, out var disabledReason)) {
            if (showFailureMessage && !disabledReason.NullOrEmpty()) {
                Messages.Message(disabledReason, pawn, MessageTypeDefOf.RejectInput, historical: false);
            }

            return false;
        }

        var switchFireModeJob = JobMaker.MakeJob(JobDefOf.CRTeam_SwitchFireMode, pawn, parent);
        switchFireModeJob.count = (int)targetMode;
        pawn.jobs.StartJob(switchFireModeJob, JobCondition.InterruptForced, resumeCurJobAfterwards: true,
            tag: JobTag.Misc);
        return true;
    }

    private bool CanSwitchToFireMode(Pawn pawn, FireMode targetMode, out string disabledReason) {
        if (!CanOpenSwitchFireModeMenu(pawn, out disabledReason)) {
            return false;
        }

        if (!SupportsMode(targetMode)) {
            disabledReason = "当前武器不支持该模式";
            return false;
        }

        if (_currentMode == targetMode) {
            disabledReason = "已经是该模式";
            return false;
        }

        disabledReason = string.Empty;
        return true;
    }

    private FireMode GetDefaultMode() {
        return HasBurstMode() ? FireMode.Burst : FireMode.Single;
    }

    private void ShowSwitchFireModeMenu(Pawn pawn) {
        if (!CanOpenSwitchFireModeMenu(pawn, out var disabledReason)) {
            if (!disabledReason.NullOrEmpty()) {
                Messages.Message(disabledReason, pawn, MessageTypeDefOf.RejectInput, historical: false);
            }

            return;
        }

        var options = GetSupportedModes()
            .Select(mode => mode == _currentMode
                ? new FloatMenuOption($"{GetModeLabel(mode)} (当前)", null)
                : new FloatMenuOption(GetModeLabel(mode), () => TryStartSwitchFireModeJob(pawn, mode)))
            .ToList();

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private IEnumerable<FireMode> GetSupportedModes() {
        yield return FireMode.Single;

        if (HasBurstMode()) {
            yield return FireMode.Burst;
        }

        yield return FireMode.Auto;
    }

    private bool HasBurstMode() {
        return parent.def.Verbs != null && Enumerable.Any(parent.def.Verbs, verb => verb.burstShotCount > 1);
    }

    public bool SupportsMode(FireMode mode) {
        return mode switch {
            FireMode.Single => true,
            FireMode.Burst => HasBurstMode(),
            FireMode.Auto => true,
            _ => false,
        };
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

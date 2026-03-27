using Verse.AI;

namespace CombatRefactor;

public class CompMagazine : ThingComp, IEquippedGizmoProvider {
    private int _remainingShots;

    private CompProperties_Magazine Props => (CompProperties_Magazine)props;

    public int RemainingShots => _remainingShots;

    public int MagazineCapacity => ResolveMagazineCapacity();

    public int ReloadTicks => Math.Max(1, Props.reloadTicks);

    public bool Empty => _remainingShots <= 0;

    public bool NeedsReload => _remainingShots < MagazineCapacity;

    public override void Initialize(CompProperties properties) {
        base.Initialize(properties);
        _remainingShots = MagazineCapacity;
    }

    public override void PostExposeData() {
        base.PostExposeData();
        var defaultShots = MagazineCapacity;
        Scribe_Values.Look(ref _remainingShots, "remainingShots", defaultShots);
    }

    public override void Notify_UsedWeapon(Pawn pawn) {
        base.Notify_UsedWeapon(pawn);
        if (_remainingShots <= 0) {
            return;
        }

        _remainingShots--;
    }

    public override string CompInspectStringExtra() {
        return $"弹匣: {RemainingShots} / {MagazineCapacity}";
    }

    public IEnumerable<Gizmo> GetEquippedGizmos() {
        var pawn = GetEquippingPawn();
        if (pawn == null || !CanShowFor(pawn)) {
            yield break;
        }

        if (!PawnAttackGizmoUtility.CanShowEquipmentGizmos()) {
            yield break;
        }

        var reloadCommand = new Command_Action {
            defaultLabel = $"装填 ({RemainingShots}/{MagazineCapacity})",
            defaultDesc = $"装填当前武器的弹匣。\n耗时: {ReloadTicks.ToStringTicksToPeriod(shortForm: true)}",
            icon = TexCommand.Attack,
            activateSound = SoundDefOf.Click,
            action = () => StartReload(pawn),
        };

        if (!CanReload(pawn, out var disabledReason)) {
            reloadCommand.Disable(disabledReason);
        }

        yield return reloadCommand;
    }

    private bool IsReloading(Pawn pawn) {
        return pawn.CurJob?.def == JobDefOf.CRTeam_ReloadMagazine &&
               pawn.CurJob.targetB.Thing == parent;
    }

    public bool IsHeldBy(Pawn pawn) {
        return pawn.equipment?.Primary == parent && GetEquippingPawn() == pawn;
    }

    private bool CanReload(Pawn pawn, out string disabledReason) {
        if (!IsHeldBy(pawn)) {
            disabledReason = "未装备当前武器";
            return false;
        }

        if (IsReloading(pawn)) {
            disabledReason = "正在装填";
            return false;
        }

        if (!NeedsReload) {
            disabledReason = "弹匣已满";
            return false;
        }

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) {
            disabledReason = "无法操作武器";
            return false;
        }

        disabledReason = string.Empty;
        return true;
    }

    private void StartReload(Pawn pawn) {
        if (!CanReload(pawn, out var disabledReason)) {
            if (!disabledReason.NullOrEmpty()) {
                Messages.Message(disabledReason, pawn, MessageTypeDefOf.RejectInput, historical: false);
            }

            return;
        }

        var reloadJob = JobMaker.MakeJob(JobDefOf.CRTeam_ReloadMagazine, pawn, parent);
        pawn.jobs.TryTakeOrderedJob(reloadJob, JobTag.Misc);
    }

    public void CompleteReload() {
        _remainingShots = MagazineCapacity;
    }

    private int ResolveMagazineCapacity() {
        if (Props.magazineCapacity > 0) {
            return Props.magazineCapacity;
        }

        var burstShotCount = 1;
        if (parent.def.Verbs != null) {
            burstShotCount = parent.def.Verbs
                .Select(verb => verb.burstShotCount)
                .Prepend(burstShotCount)
                .Max();
        }

        return Math.Max(1, burstShotCount * Props.burstShotCountCapacityMultiplier);
    }

    private Pawn? GetEquippingPawn() {
        return parent.ParentHolder is Pawn_EquipmentTracker equipmentTracker ? equipmentTracker.pawn : null;
    }

    private static bool CanShowFor(Pawn pawn) {
        return pawn.drafter is { Drafted: true } &&
               (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled ||
                pawn.IsColonySubhumanPlayerControlled);
    }
}

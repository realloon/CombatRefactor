namespace CombatRefactor;

public class CompMagazine : ThingComp, IEquippedGizmoProvider {
    private int _remainingShots;

    private CompProperties_Magazine Props => (CompProperties_Magazine)props;

    public int RemainingShots => _remainingShots;

    public int MagazineCapacity => ResolveMagazineCapacity();

    public bool Empty => _remainingShots <= 0;

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
            defaultDesc = "将当前武器的弹匣装满。",
            icon = TexCommand.Attack,
            activateSound = SoundDefOf.Click,
            action = Reload,
        };

        if (RemainingShots >= MagazineCapacity) {
            reloadCommand.Disable("弹匣已满");
        }

        yield return reloadCommand;
    }

    private void Reload() {
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
        return pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled ||
               pawn.IsColonySubhumanPlayerControlled;
    }
}
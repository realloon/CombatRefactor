using JetBrains.Annotations;
using Verse.AI;

namespace CombatRefactor;

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
public class JobDriver_SwitchFireMode : JobDriver {
    private const TargetIndex ActorInd = TargetIndex.A;
    private const TargetIndex WeaponInd = TargetIndex.B;

    private ThingWithComps? Weapon => job.GetTarget(WeaponInd).Thing as ThingWithComps;

    private CompFireSelector? FireSelector => Weapon?.TryGetComp<CompFireSelector>();

    private FireMode TargetMode => (FireMode)job.count;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils() {
        this.FailOn(() => FireSelector == null);
        this.FailOn(() => Weapon == null);
        this.FailOn(() => !FireSelector!.IsHeldBy(pawn));
        this.FailOn(() => !FireSelector!.SupportsMode(TargetMode));
        this.FailOn(() => FireSelector!.CurrentMode == TargetMode);
        this.FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));

        var wait = Toils_General.Wait(CompFireSelector.SwitchFireModeTicks);
        wait.WithProgressBarToilDelay(ActorInd);
        yield return wait;

        yield return Toils_General.Do(() => FireSelector?.CompleteSwitchFireMode(TargetMode));
    }
}
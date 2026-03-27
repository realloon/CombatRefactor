using JetBrains.Annotations;
using Verse.AI;

namespace CombatRefactor;

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
public class JobDriver_ReloadMagazine : JobDriver {
    private const TargetIndex ActorInd = TargetIndex.A;
    private const TargetIndex WeaponInd = TargetIndex.B;

    private ThingWithComps? Weapon => job.GetTarget(WeaponInd).Thing as ThingWithComps;

    private CompMagazine? Magazine => Weapon?.TryGetComp<CompMagazine>();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils() {
        this.FailOn(() => Magazine == null);
        this.FailOn(() => Weapon == null);
        this.FailOn(() => !Magazine!.IsHeldBy(pawn));
        this.FailOn(() => !Magazine!.NeedsReload);
        this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

        var wait = Toils_General.Wait(Magazine!.ReloadTicks);
        wait.WithProgressBarToilDelay(ActorInd);
        yield return wait;

        yield return Toils_General.Do(() => Magazine?.CompleteReload());
    }
}
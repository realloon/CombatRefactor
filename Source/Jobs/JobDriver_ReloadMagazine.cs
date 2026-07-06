using JetBrains.Annotations;
using Verse.AI;
using CombatRefactor.Utility;

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
        var magazine = Magazine ?? throw new InvalidOperationException("Reload job requires CompMagazine target.");

        this.FailOn(() => !magazine.IsHeldBy(pawn));
        this.FailOn(() => !magazine.NeedsReload);
        this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
        AddFinishAction(_ => SuspendedAttackJobStateUtility.Clear(pawn));

        var wait = Toils_General.Wait(magazine.ReloadTicks);
        wait.WithProgressBarToilDelay(ActorInd);
        yield return wait;

        yield return Toils_General.Do(() => {
            magazine.CompleteReload();
            SuspendedAttackJobStateUtility.DiscardInvalidQueuedAttack(pawn);
        });
    }
}
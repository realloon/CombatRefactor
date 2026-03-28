using System.Runtime.CompilerServices;
using Verse.AI;

namespace CombatRefactor.Utility;

public static class SuspendedAttackJobStateUtility {
    private sealed class SuspendedAttackState {
        public Job SuspendedJob { get; set; } = null!;

        public bool TargetWasDowned { get; set; }
    }

    private static readonly ConditionalWeakTable<Pawn, SuspendedAttackState> SuspendedStates = new();

    public static void Record(Pawn pawn, Job? job) {
        Clear(pawn);
        if (job?.def != RimWorld.JobDefOf.AttackStatic || job.targetA.Thing is not Pawn targetPawn) {
            return;
        }

        SuspendedStates.Add(pawn, new SuspendedAttackState {
            SuspendedJob = job,
            TargetWasDowned = targetPawn.Downed
        });
    }

    public static void DiscardInvalidQueuedAttack(Pawn pawn) {
        if (!SuspendedStates.TryGetValue(pawn, out var suspendedState)) {
            return;
        }

        Clear(pawn);
        if (suspendedState.TargetWasDowned ||
            suspendedState.SuspendedJob.targetA.Thing is not Pawn targetPawn ||
            !targetPawn.Downed) {
            return;
        }

        var queuedJob = pawn.jobs.jobQueue.Extract(suspendedState.SuspendedJob);
        queuedJob?.Cleanup(pawn, canReturnToPool: true);
    }

    public static void Clear(Pawn pawn) {
        SuspendedStates.Remove(pawn);
    }
}
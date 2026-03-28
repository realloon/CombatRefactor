using System.Runtime.CompilerServices;
using Verse.AI;

namespace CombatRefactor.Utility;

public static class SuspendedAttackJobStateUtility {
    private sealed class SuspendedAttackState {
        public bool StartedIncapacitated { get; set; }
    }

    private static readonly ConditionalWeakTable<Job, SuspendedAttackState> SuspendedStates = new();

    public static void Record(Job? job) {
        if (job?.def != RimWorld.JobDefOf.AttackStatic || job.targetA.Thing is not Pawn targetPawn) {
            return;
        }

        SuspendedStates.Remove(job);
        SuspendedStates.Add(job, new SuspendedAttackState {
            StartedIncapacitated = targetPawn.Downed
        });
    }

    public static bool TryConsume(Job? job, out bool startedIncapacitated) {
        startedIncapacitated = false;
        if (job == null || !SuspendedStates.TryGetValue(job, out var suspendedState)) {
            return false;
        }

        startedIncapacitated = suspendedState.StartedIncapacitated;
        SuspendedStates.Remove(job);
        return true;
    }
}
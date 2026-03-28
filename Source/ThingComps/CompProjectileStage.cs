namespace CombatRefactor;

public class CompProjectileStage : ThingComp {
    private float _coverInterceptRoll = -1f;
    private IntVec3 _pendingFlightDestination = IntVec3.Invalid;
    private IntVec3 _flightDestination = IntVec3.Invalid;
    private bool _usesTargetLeanExposure;

    public float CoverInterceptRoll => _coverInterceptRoll;

    public bool HasCoverInterceptRoll => _coverInterceptRoll >= 0f;
    public bool HasPendingFlightDestination => _pendingFlightDestination.IsValid;
    public IntVec3 PendingFlightDestination => _pendingFlightDestination;
    public bool HasFlightDestination => _flightDestination.IsValid;
    public IntVec3 FlightDestination => _flightDestination;
    public bool UsesTargetLeanExposure => _usesTargetLeanExposure;

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Values.Look(ref _coverInterceptRoll, "coverInterceptRoll", -1f);
        Scribe_Values.Look(ref _pendingFlightDestination, "pendingFlightDestination", IntVec3.Invalid);
        Scribe_Values.Look(ref _flightDestination, "flightDestination", IntVec3.Invalid);
        Scribe_Values.Look(ref _usesTargetLeanExposure, "usesTargetLeanExposure");
    }

    public void RollCoverIntercept() {
        _coverInterceptRoll = Rand.Value;
    }

    public void SetPendingFlightDestination(IntVec3 destinationCell) {
        _pendingFlightDestination = destinationCell;
    }

    public void SetFlightDestination(IntVec3 destinationCell) {
        _flightDestination = destinationCell;
    }

    public void SetUsesTargetLeanExposure(bool value) {
        _usesTargetLeanExposure = value;
    }

    public void ClearPendingFlightDestination() {
        _pendingFlightDestination = IntVec3.Invalid;
    }
}
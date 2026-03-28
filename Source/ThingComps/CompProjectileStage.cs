namespace CombatRefactor;

public class CompProjectileStage : ThingComp {
    private float _coverInterceptRoll = -1f;
    private IntVec3 _pendingFlightSource = IntVec3.Invalid;
    private IntVec3 _flightSource = IntVec3.Invalid;
    private IntVec3 _pendingFlightDestination = IntVec3.Invalid;
    private IntVec3 _flightDestination = IntVec3.Invalid;
    private IntVec3 _pendingProtectedLeanSupportCell = IntVec3.Invalid;
    private IntVec3 _protectedLeanSupportCell = IntVec3.Invalid;
    private bool _usesTargetLeanExposure;

    public float CoverInterceptRoll => _coverInterceptRoll;

    public bool HasCoverInterceptRoll => _coverInterceptRoll >= 0f;
    public bool HasPendingFlightSource => _pendingFlightSource.IsValid;
    public IntVec3 PendingFlightSource => _pendingFlightSource;
    public bool HasFlightSource => _flightSource.IsValid;
    public IntVec3 FlightSource => _flightSource;
    public bool HasPendingFlightDestination => _pendingFlightDestination.IsValid;
    public IntVec3 PendingFlightDestination => _pendingFlightDestination;
    public bool HasFlightDestination => _flightDestination.IsValid;
    public IntVec3 FlightDestination => _flightDestination;
    public bool HasPendingProtectedLeanSupportCell => _pendingProtectedLeanSupportCell.IsValid;
    public IntVec3 PendingProtectedLeanSupportCell => _pendingProtectedLeanSupportCell;
    public bool HasProtectedLeanSupportCell => _protectedLeanSupportCell.IsValid;
    public IntVec3 ProtectedLeanSupportCell => _protectedLeanSupportCell;
    public bool UsesTargetLeanExposure => _usesTargetLeanExposure;

    public void RollCoverIntercept() {
        _coverInterceptRoll = Rand.Value;
    }

    public void SetPendingFlightSource(IntVec3 sourceCell) {
        _pendingFlightSource = sourceCell;
    }

    public void SetFlightSource(IntVec3 sourceCell) {
        _flightSource = sourceCell;
    }

    public void SetPendingFlightDestination(IntVec3 destinationCell) {
        _pendingFlightDestination = destinationCell;
    }

    public void SetFlightDestination(IntVec3 destinationCell) {
        _flightDestination = destinationCell;
    }

    public void SetPendingProtectedLeanSupportCell(IntVec3 cell) {
        _pendingProtectedLeanSupportCell = cell;
    }

    public void SetProtectedLeanSupportCell(IntVec3 cell) {
        _protectedLeanSupportCell = cell;
    }

    public void SetUsesTargetLeanExposure(bool value) {
        _usesTargetLeanExposure = value;
    }

    public void ClearPendingFlightDestination() {
        _pendingFlightDestination = IntVec3.Invalid;
    }

    public void ClearPendingFlightSource() {
        _pendingFlightSource = IntVec3.Invalid;
    }

    public void ClearPendingProtectedLeanSupportCell() {
        _pendingProtectedLeanSupportCell = IntVec3.Invalid;
    }

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Values.Look(ref _coverInterceptRoll, "coverInterceptRoll", -1f);
        Scribe_Values.Look(ref _pendingFlightSource, "pendingFlightSource", IntVec3.Invalid);
        Scribe_Values.Look(ref _flightSource, "flightSource", IntVec3.Invalid);
        Scribe_Values.Look(ref _pendingFlightDestination, "pendingFlightDestination", IntVec3.Invalid);
        Scribe_Values.Look(ref _flightDestination, "flightDestination", IntVec3.Invalid);
        Scribe_Values.Look(ref _pendingProtectedLeanSupportCell, "pendingProtectedLeanSupportCell", IntVec3.Invalid);
        Scribe_Values.Look(ref _protectedLeanSupportCell, "protectedLeanSupportCell", IntVec3.Invalid);
        Scribe_Values.Look(ref _usesTargetLeanExposure, "usesTargetLeanExposure");
    }
}
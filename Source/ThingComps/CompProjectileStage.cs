namespace CombatRefactor;

public class CompProjectileStage : ThingComp {
    private float _coverInterceptRoll = -1f;

    public float CoverInterceptRoll => _coverInterceptRoll;

    public bool HasCoverInterceptRoll => _coverInterceptRoll >= 0f;

    public override void PostExposeData() {
        base.PostExposeData();
        Scribe_Values.Look(ref _coverInterceptRoll, "coverInterceptRoll", -1f);
    }

    public void RollCoverIntercept() {
        _coverInterceptRoll = Rand.Value;
    }
}
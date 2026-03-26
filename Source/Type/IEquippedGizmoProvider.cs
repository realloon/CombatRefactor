namespace CombatRefactor.Type;

public interface IEquippedGizmoProvider {
    IEnumerable<Gizmo> GetEquippedGizmos();
}

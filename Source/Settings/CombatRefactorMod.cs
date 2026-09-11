using JetBrains.Annotations;
namespace CombatRefactor;

[UsedImplicitly]
public sealed class CombatRefactorMod : Mod {
    public static CombatRefactorSettings Settings = null!;

    public CombatRefactorMod(ModContentPack content) : base(content) {
        Settings = GetSettings<CombatRefactorSettings>();
    }

    public override string SettingsCategory() => "CombatRefactor";

}

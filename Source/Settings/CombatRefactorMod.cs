using JetBrains.Annotations;
using UnityEngine;

namespace CombatRefactor;

[UsedImplicitly]
public sealed class CombatRefactorMod : Mod {
    public static CombatRefactorSettings Settings = null!;

    public CombatRefactorMod(ModContentPack content) : base(content) {
        Settings = GetSettings<CombatRefactorSettings>();
    }

    public override string SettingsCategory() => "CombatRefactor";

    public override void DoSettingsWindowContents(Rect inRect) {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        base.DoSettingsWindowContents(inRect);
    }
}
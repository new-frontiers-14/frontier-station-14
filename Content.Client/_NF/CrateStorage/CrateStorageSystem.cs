using Content.Shared._NF.CrateStorage;
using Robust.Client.GameObjects;

namespace Content.Client._NF.CrateStorage;

public sealed class CrateStorageRackSystem: VisualizerSystem<CrateStorageRackComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CrateStorageRackComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData<int>(uid, CrateStorageRackVisuals.VisualState, out var storedCrates, args.Component);
        args.Sprite.LayerSetVisible(CrateStorageRackVisualLayers.Base, true);
        args.Sprite.LayerSetVisible(CrateStorageRackVisualLayers.Top, storedCrates > 1);
        args.Sprite.LayerSetVisible(CrateStorageRackVisualLayers.Bottom, storedCrates > 0);
    }
}

public enum CrateStorageRackVisualLayers : byte
{
    Base,
    Top,
    Bottom,
}

using Content.Shared._NF.CrateStorage;
using Robust.Client.GameObjects;

namespace Content.Client._NF.CrateStorageRack;

public sealed class CrateStorageRackSystem : SharedCrateStorageMachineSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrateStorageRackComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CrateStorageRackComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentInit(EntityUid uid, CrateStorageRackComponent crateMachine, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        UpdateState(uid, crateMachine, sprite, appearance);
    }

    /// <summary>
    /// Update visuals and tick animation
    /// </summary>
    private void UpdateState(EntityUid uid, CrateStorageRackComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        sprite.LayerSetVisible(CrateStorageRackVisualLayers.Base, true);
        sprite.LayerSetVisible(CrateStorageRackVisualLayers.Top, component.StoredCrates > 1);
        sprite.LayerSetVisible(CrateStorageRackVisualLayers.Bottom, component.StoredCrates > 0);
    }

    private void OnAppearanceChange(EntityUid uid, CrateStorageRackComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(uid, component, args.Sprite, args.Component);
    }
}

public enum CrateStorageRackVisualLayers : byte
{
    Base,
    Top,
    Bottom,
}

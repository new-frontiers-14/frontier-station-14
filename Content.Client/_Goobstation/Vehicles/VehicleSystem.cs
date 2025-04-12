using System.Numerics;
using Content.Shared.Vehicles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SpriteSystem _sprites = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMove);
    }

    private void OnAppearanceChange(EntityUid uid, VehicleComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !_appearance.TryGetData<bool>(uid, VehicleState.Animated, out bool animated) || !TryComp<SpriteComponent>(uid, out var spriteComp))
            return;

        SpritePos(uid, comp);

        // Frontier: handle arbitrary animated layers
        if (!spriteComp.LayerMapTryGet(VehicleVisualLayers.AutoAnimate, out var layer))
            layer = 0;
        spriteComp.LayerSetAutoAnimated(layer, animated);
        // End Frontier
    }

    private void OnMove(EntityUid uid, VehicleComponent component, ref MoveEvent args)
    {
        SpritePos(uid, component);
    }

    private void SpritePos(EntityUid uid, VehicleComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComp))
            return;

        if (!_appearance.TryGetData<bool>(uid, VehicleState.DrawOver, out bool depth))
            return;

        spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;

        if (comp.RenderOver == VehicleRenderOver.None)
            return;

        var eye = _eye.CurrentEye;
        Direction vehicleDir = (Transform(uid).LocalRotation + eye.Rotation).GetCardinalDir();

        VehicleRenderOver renderOver = (VehicleRenderOver)(1 << (int)vehicleDir);

        if ((comp.RenderOver & renderOver) == renderOver)
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.OverMobs;
        }
        else
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;
        }
    }
}

// Frontier: restore the autoanimated layer
public enum VehicleVisualLayers : byte
{
    /// Layer for the vehicle's wheels/jets/etc.
    AutoAnimate,
}
// End Frontier

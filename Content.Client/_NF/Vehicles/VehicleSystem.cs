using System.Numerics;
using Content.Shared._Goobstation.Vehicles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;

namespace Content.Client._NF.Vehicles;

// Rewritten from Goobstation's VehicleSystem.
public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, VehicleComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !_appearance.TryGetData(uid, VehicleState.Animated, out bool animated)
            || !TryComp<SpriteComponent>(uid, out var spriteComp))
        {
            return;
        }

        if (!spriteComp.LayerMapTryGet(VehicleVisualLayers.AutoAnimate, out var layer))
            layer = 0;
        spriteComp.LayerSetAutoAnimated(layer, animated);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<VehicleComponent, SpriteComponent>();
        var eye = _eye.CurrentEye;
        while (query.MoveNext(out var uid, out var vehicle, out var sprite))
        {
            var angle = _transform.GetWorldRotation(uid) + eye.Rotation;
            if (angle < 0)
                angle += 2 * Math.PI;
            RsiDirection dir = SpriteComponent.Layer.GetDirection(RsiDirectionType.Dir4, angle);
            VehicleRenderOver renderOver = (VehicleRenderOver)(1 << (int)dir);

            if ((vehicle.RenderOver & renderOver) == renderOver)
                sprite.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.OverMobs;
            else
                sprite.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;

            Vector2 offset = Vector2.Zero;
            if (vehicle.Driver != null)
            {
                switch (dir)
                {
                    case RsiDirection.South:
                    default:
                        offset = vehicle.SouthOffset;
                        break;
                    case RsiDirection.North:
                        offset = vehicle.NorthOffset;
                        break;
                    case RsiDirection.East:
                        offset = vehicle.EastOffset;
                        break;
                    case RsiDirection.West:
                        offset = vehicle.WestOffset;
                        break;
                }
            }

            // Avoid recalculating a matrix if we can help it.
            if (sprite.Offset != offset)
                sprite.Offset = offset;
        }
    }
}

public enum VehicleVisualLayers : byte
{
    /// Layer for the vehicle's wheels/jets/etc.
    AutoAnimate,
}

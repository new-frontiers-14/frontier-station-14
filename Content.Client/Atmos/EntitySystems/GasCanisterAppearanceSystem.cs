// Upstream#37341

using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// Used to change the appearance of gas canisters.
/// </summary>
public sealed class GasCanisterAppearanceSystem : VisualizerSystem<GasCanisterComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    protected override void OnAppearanceChange(EntityUid uid, GasCanisterComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<string>(uid, PaintableVisuals.Prototype, out var protoName, args.Component) || args.Sprite is not { } old)
            return;

        if (!_prototypeManager.HasIndex(protoName))
            return;

        // Create the given prototype and get its first layer.
        var tempUid = Spawn(protoName);
        // Frontier: older sprite functions
        if (TryComp<SpriteComponent>(tempUid, out var sprite))
            old.LayerSetState(0, sprite.LayerGetState(0));
        // End Frontier: older sprite functions
        QueueDel(tempUid);
    }
}

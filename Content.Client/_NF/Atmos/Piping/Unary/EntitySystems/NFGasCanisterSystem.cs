using Content.Client._NF.Atmos.Piping.Unary.Components;
using Content.Shared._NF.NFSprayPainter.Prototypes;
using Robust.Client.GameObjects;

namespace Content.Client._NF.Atmos.Piping.Unary.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class NFGasCanisterSystem : VisualizerSystem<NFGasCanisterVisualsComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnAppearanceChange(EntityUid uid, NFGasCanisterVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Frontier: SprayPainter Start
        if (AppearanceSystem.TryGetData<string>(uid, NFPaintableVisuals.CanisterState, out var canisterState, args.Component))
        {
            if (args.Sprite.TryGetLayer(0, out var layer))
            {
                layer.SetState(canisterState);
            }
        }
        // Frontier: SprayPainter End
    }
}

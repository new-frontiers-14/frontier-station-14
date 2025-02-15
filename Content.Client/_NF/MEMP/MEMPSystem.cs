using Content.Shared._NF.MEMP;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client._NF.MEMP;

public sealed partial class MEMPSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedMEMPGeneratorComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    /// <summary>
    /// Ensures that the visible state of gravity generators are synced with their sprites.
    /// </summary>
    private void OnAppearanceChange(EntityUid uid, SharedMEMPGeneratorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearanceSystem.TryGetData<PowerChargeStatus>(uid, PowerChargeVisuals.State, out var state, args.Component))
        {
            if (comp.SpriteMap.TryGetValue(state, out var spriteState))
            {
                var layer = args.Sprite.LayerMapGet(MEMPGeneratorVisualLayers.Base);
                args.Sprite.LayerSetState(layer, spriteState);
            }
        }

        if (_appearanceSystem.TryGetData<float>(uid, PowerChargeVisuals.Charge, out var charge, args.Component))
        {
            var layer = args.Sprite.LayerMapGet(MEMPGeneratorVisualLayers.Core);
            switch (charge)
            {
                case < 0.2f:
                    args.Sprite.LayerSetVisible(layer, false);
                    break;
                case >= 0.2f and < 0.4f:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, comp.CoreStartupState);
                    break;
                case >= 0.4f and < 0.6f:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, comp.CoreIdleState);
                    break;
                case >= 0.6f and < 0.8f:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, comp.CoreActivatingState);
                    break;
                default:
                    args.Sprite.LayerSetVisible(layer, true);
                    args.Sprite.LayerSetState(layer, comp.CoreActivatedState);
                    break;
            }
        }
    }
}

public enum MEMPGeneratorVisualLayers : byte
{
    Base,
    Core
}

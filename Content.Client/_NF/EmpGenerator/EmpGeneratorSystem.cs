using Content.Shared._NF.EmpGenerator;
using Content.Shared.Power;
using Robust.Client.GameObjects;

namespace Content.Client._NF.EmpGenerator;

public sealed partial class EmpGeneratorSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpGeneratorVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    /// <summary>
    /// Ensures that the visible state of mobile emps are synced with their sprites.
    /// </summary>
    private void OnAppearanceChange(EntityUid uid, EmpGeneratorVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearanceSystem.TryGetData<PowerChargeStatus>(uid, PowerChargeVisuals.State, out var state, args.Component))
        {
            if (comp.SpriteMap.TryGetValue(state, out var spriteState))
            {
                var layer = args.Sprite.LayerMapGet(EmpGeneratorVisualLayers.Base);
                args.Sprite.LayerSetState(layer, spriteState);
            }
        }

        if (_appearanceSystem.TryGetData<float>(uid, PowerChargeVisuals.Charge, out var charge, args.Component))
        {
            var layer = args.Sprite.LayerMapGet(EmpGeneratorVisualLayers.Core);
            foreach (var threshold in comp.Thresholds)
            {
                if (charge < threshold.MaxCharge)
                {
                    args.Sprite.LayerSetVisible(layer, threshold.Visible);
                    if (threshold.State != null)
                        args.Sprite.LayerSetState(layer, threshold.State);
                    break;
                }
            }
        }
    }
}

public enum EmpGeneratorVisualLayers : byte
{
    Base,
    Core
}

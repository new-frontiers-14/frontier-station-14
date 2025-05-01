using Content.Client._NF.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client._NF.Charges.Systems;

// Limited charge visualizer - essentially a copy of the magazine visuals.
public sealed partial class LimitedChargesVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimitedChargesVisualsComponent, ComponentInit>(OnChargeVisualsInit);
        SubscribeLocalEvent<LimitedChargesVisualsComponent, AppearanceChangeEvent>(OnMagazineVisualsChange);
    }

    private void OnChargeVisualsInit(EntityUid uid, LimitedChargesVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)) return;

        if (sprite.LayerMapTryGet(LimitedChargesVisualLayers.Charges, out var chargeLayer))
        {
            sprite.LayerSetState(chargeLayer, $"{component.ChargePrefix}-{component.ChargeSteps - 1}");
            sprite.LayerSetVisible(chargeLayer, false);
        }
    }

    private void OnMagazineVisualsChange(EntityUid uid, LimitedChargesVisualsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null) return;

        if (!args.AppearanceData.TryGetValue(LimitedChargeVisuals.MaxCharges, out var capacity))
            capacity = component.ChargeSteps;

        if (!args.AppearanceData.TryGetValue(LimitedChargeVisuals.Charges, out var current))
            current = component.ChargeSteps;

        var step = ContentHelpers.RoundToLevels((int)current, (int)capacity, component.ChargeSteps);

        int chargeLayer;
        if (step == 0 && !component.ZeroVisible)
        {
            if (sprite.LayerMapTryGet(LimitedChargesVisualLayers.Charges, out chargeLayer))
                sprite.LayerSetVisible(chargeLayer, false);
        }
        else if (sprite.LayerMapTryGet(LimitedChargesVisualLayers.Charges, out chargeLayer))
        {
            sprite.LayerSetVisible(chargeLayer, true);
            sprite.LayerSetState(chargeLayer, $"{component.ChargePrefix}-{step}");
        }
    }
}

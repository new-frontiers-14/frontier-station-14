using Content.Client._NF.Power.Components;
using Content.Shared._NF.Power.Components;
using Robust.Client.GameObjects;

namespace Content.Client._NF.Power.EntitySystems;

public sealed class EmergencyChargeSystem : VisualizerSystem<EmergencyChargeComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, EmergencyChargeComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, EmergencyChargeVisuals.On, out var on, args.Component))
            on = false;
    }
}

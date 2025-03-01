using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Robust.Client.GameObjects;
using static Content.Shared.DeviceLinking.RngDeviceVisuals;

namespace Content.Client.DeviceLinking.Systems;

public sealed class RngDeviceVisualizerSystem : VisualizerSystem<RngDeviceVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RngDeviceVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (args.AppearanceData.TryGetValue(State, out var state) && state is string stateVal)
        {
            sprite.LayerSetState("dice", stateVal);
        }
    }
}
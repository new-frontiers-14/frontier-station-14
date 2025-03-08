using Content.Shared._NF.DeviceLinking;
using Content.Shared._NF.DeviceLinking.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared._NF.DeviceLinking.RngDeviceVisuals;

namespace Content.Client._NF.DeviceLinking.Systems;

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

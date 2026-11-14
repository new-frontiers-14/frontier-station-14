using Content.Shared._WF.SafetyDepositBox;
using Robust.Client.GameObjects;

namespace Content.Client._WF.SafetyDepositBox;

public sealed class SafetyDepositConsoleVisualizerSystem : VisualizerSystem<AppearanceComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AppearanceComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), SafetyDepositConsoleVisualLayers.Printing, out var layer, false))
            return;

        AppearanceSystem.TryGetData<bool>(uid, SafetyDepositConsoleVisuals.Printing, out var printing);

        SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, printing);
    }
}

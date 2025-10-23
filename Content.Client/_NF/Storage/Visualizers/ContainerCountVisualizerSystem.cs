using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;

namespace Content.Client._NF.Storage.Visualizers;

public sealed class ContainerCountVisualizerSystem : VisualizerSystem<ContainerCountVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ContainerCountVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!HasComp<SpriteComponent>(uid))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StorageFillVisuals.FillLevel, out var level, args.Component))
            return;

        var state = $"{component.FillBaseName}-{level}";
        args.Sprite.LayerSetState(StorageFillLayers.Fill, state);
    }
}

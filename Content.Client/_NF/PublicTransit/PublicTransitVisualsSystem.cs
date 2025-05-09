using Content.Shared._NF.PublicTransit;
using Content.Shared._NF.PublicTransit.Components;
using Robust.Client.GameObjects;

namespace Content.Client._NF.PublicTransit;

/// <summary>
/// If enabled, spawns a public trasnport grid as definied by cvar, to act as an automatic transit shuttle between designated grids
/// </summary>
public sealed class PublicTransitSystem : VisualizerSystem<PublicTransitVisualsComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, PublicTransitVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !_appearance.TryGetData(uid, PublicTransitVisuals.Livery, out Color color)
            || !args.Sprite.LayerMapTryGet(PublicTransitVisualLayers.Livery, out int layer))
        {
            return;
        }

        args.Sprite.LayerSetColor(layer, color);
    }
}

using System.Linq;
using Content.Client.SubFloor;
using Robust.Client.GameObjects;

namespace Content.Client._NF.Markers;

/// <summary>
/// Shows and hides sprite layers depending on whether sandbox markers are visible.
/// </summary>
public sealed class MarkerLayersSystem : EntitySystem
{
    [Dependency] private SpriteSystem _spriteSystem = default!;

    private static string _overlayKeyPrefix = "NF.Markers.SpriteOverlay.";

    private bool _overlaysVisible;

    public bool OverlaysVisible
    {
        get => _overlaysVisible;
        set
        {
            _overlaysVisible = value;
            UpdateOverlays();
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarkerLayersComponent, ComponentStartup>(OnStartup);
        // The subfloor system hides all layers except for some that are listed by enum. We have arbitrary keys, so
        // an enum wouldn't do; instead, we refresh the marker layers after the subfloor system has done its work.
        SubscribeLocalEvent<MarkerLayersComponent, AppearanceChangeEvent>(OnAppearanceChanged, after: [typeof(SubFloorHideSystem)]);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        OverlaysVisible = false;
    }

    private void OnStartup(EntityUid uid, MarkerLayersComponent component, ComponentStartup args)
    {
        UpdateLayers(uid, component);
    }

    private void OnAppearanceChanged(EntityUid uid, MarkerLayersComponent component, ref AppearanceChangeEvent args)
    {
        UpdateLayers(uid, component);
    }

    private void UpdateLayers(EntityUid uid, MarkerLayersComponent? component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref component, ref sprite))
        {
            return;
        }

        Entity<SpriteComponent?> entity = (uid, sprite);

        // Remove layers we added in a prior pass
        foreach (var layer in component.LayerKeys)
        {
            _spriteSystem.RemoveLayer(entity, layer);
        }
        component.LayerKeys.Clear();

        if (!OverlaysVisible)
        {
            return;
        }

        // Add any sandbox-only layers
        foreach (var (defIndex, defData) in component.Layers.Index())
        {
            var index = _spriteSystem.AddLayer(entity, defData, null);
            var key = _overlayKeyPrefix + defIndex;
            _spriteSystem.LayerMapSet(entity, key, index);
            component.LayerKeys.Add(key);
        }
    }

    private void UpdateOverlays()
    {
        var query = AllEntityQuery<MarkerLayersComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var overlay, out var sprite))
        {
            UpdateLayers(uid, overlay, sprite);
        }
    }
}

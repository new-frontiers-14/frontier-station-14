using Content.Shared._Park.Species.Shadowkin.Events;
using Content.Shared._Park.Species.Shadowkin.Components;
using Robust.Client.GameObjects;
using Content.Shared.Humanoid;

namespace Content.Client._Park.Species.Shadowkin.Systems;

public sealed class ShadowkinBlackeyeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShadowkinBlackeyeEvent>(OnBlackeye);

        SubscribeLocalEvent<ShadowkinComponent, ComponentInit>(OnInit);
    }

    private void OnBlackeye(ShadowkinBlackeyeEvent ev)
    {
        SetColor(ev.Uid, Color.Black);
    }


    private void OnInit(EntityUid uid, ShadowkinComponent component, ComponentInit args)
    {
        if (!_entity.TryGetComponent<SpriteComponent>(uid, out var sprite) ||
            !sprite.LayerMapTryGet(HumanoidVisualLayers.Eyes, out var index) ||
            !sprite.TryGetLayer(index, out var layer))
            return;

        // Blackeye if none of the RGB values are greater than 75
        if (layer.Color.R * 255 < 75 && layer.Color.G * 255 < 75 && layer.Color.B * 255 < 75)
        {
            RaiseNetworkEvent(new ShadowkinBlackeyeEvent(uid, false));
        }
    }


    private void SetColor(EntityUid uid, Color color)
    {
        if (!_entity.TryGetComponent<SpriteComponent>(uid, out var sprite) ||
            !sprite.LayerMapTryGet(HumanoidVisualLayers.Eyes, out var index) ||
            !sprite.TryGetLayer(index, out var layer))
            return;

        sprite.LayerSetColor(index, color);
    }
}

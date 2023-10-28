using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Client._Park.Overlays;
using Content.Client._Park.Overlays.Shaders;
using Content.Shared._Park.Species.Shadowkin.Components;
using Robust.Client.GameObjects;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;

namespace Content.Client._Park.Species.Shadowkin.Systems;

public sealed class ShadowkinTintSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    private ColorTintOverlay _tintOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _tintOverlay = new ColorTintOverlay
        {
            TintColor = new Vector3(0.5f, 0f, 0.5f),
            TintAmount = 0.25f,
            Comp = new ShadowkinComponent()
        };

        SubscribeLocalEvent<ShadowkinComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ShadowkinComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowkinComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ShadowkinComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnStartup(EntityUid uid, ShadowkinComponent component, ComponentStartup args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlay.AddOverlay(_tintOverlay);
    }

    private void OnShutdown(EntityUid uid, ShadowkinComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity != uid)
            return;

        _overlay.RemoveOverlay(_tintOverlay);
    }

    private void OnPlayerAttached(EntityUid uid, ShadowkinComponent component, PlayerAttachedEvent args)
    {
        _overlay.AddOverlay(_tintOverlay);
    }

    private void OnPlayerDetached(EntityUid uid, ShadowkinComponent component, PlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay(_tintOverlay);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _overlay.RemoveOverlay(_tintOverlay);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var uid = _player.LocalPlayer?.ControlledEntity;
        if (uid == null ||
            !_entity.TryGetComponent(uid, out ShadowkinComponent? comp) ||
            !_entity.TryGetComponent(uid, out SpriteComponent? sprite) ||
            !sprite.LayerMapTryGet(HumanoidVisualLayers.Eyes, out var index) ||
            !sprite.TryGetLayer(index, out var layer))
            return;

        // Eye color
        comp.TintColor = new Vector3(layer.Color.R, layer.Color.G, layer.Color.B);

        // 1/3 = 0.333...
        // intensity = min + (power / max)
        // intensity = intensity / 0.333
        // intensity = clamp intensity min, max
        const float min = 0.45f;
        const float max = 0.75f;
        comp.TintIntensity = Math.Clamp(min + (comp.PowerLevel / comp.PowerLevelMax) * 0.333f, min, max);

        UpdateShader(comp.TintColor, comp.TintIntensity);
    }


    private void UpdateShader(Vector3? color, float? intensity)
    {
        while (_overlay.HasOverlay<ColorTintOverlay>())
        {
            _overlay.RemoveOverlay(_tintOverlay);
        }

        if (color != null)
            _tintOverlay.TintColor = color;
        if (intensity != null)
            _tintOverlay.TintAmount = intensity;

        _overlay.AddOverlay(_tintOverlay);
    }
}

using Content.Client._NF.Body.Overlays;
using Content.Client.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Body.Systems;

/// <summary>
/// Manages a fullscreen desaturation overlay that increases as the local player loses blood.
/// The effect fades the world to grayscale (preserving the species' blood color) proportional to blood loss.
/// </summary>
public sealed class BloodlossOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private BloodlossOverlay _overlay = default!;

    /// <summary>
    /// Rate at which the overlay intensity interpolates toward the target value per second.
    /// </summary>
    private const float LerpRate = 2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnBloodstreamInit);
        SubscribeLocalEvent<BloodstreamComponent, ComponentShutdown>(OnBloodstreamShutdown);

        SubscribeLocalEvent<BloodstreamComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BloodstreamComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new BloodlossOverlay();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var localEntity = _player.LocalEntity;
        if (localEntity == null)
            return;

        if (!TryComp<BloodstreamComponent>(localEntity, out var bloodstream))
            return;

        // Update blood color from the species' blood reagent.
        if (_prototype.TryIndex<ReagentPrototype>(bloodstream.BloodReagent, out var reagent))
            _overlay.BloodColor = reagent.SubstanceColor;

        // No desaturation when dead -- other overlays handle death visuals.
        float targetIntensity;
        if (_mobState.IsDead(localEntity.Value))
        {
            targetIntensity = 0f;
        }
        else
        {
            var bloodPercentage = _bloodstream.GetBloodLevelPercentage((localEntity.Value, bloodstream));
            var threshold = bloodstream.BloodlossThreshold;

            // Scale intensity from 0 (at threshold) to 1 (at 0% blood).
            // When blood is at or above threshold, intensity is 0.
            targetIntensity = threshold > 0f
                ? Math.Clamp(1f - bloodPercentage / threshold, 0f, 1f)
                : 0f;
        }

        // Smoothly interpolate toward target to avoid jarring transitions.
        var current = _overlay.CurrentIntensity;
        _overlay.CurrentIntensity = current + (targetIntensity - current) * Math.Clamp(LerpRate * frameTime, 0f, 1f);

        // Snap to zero when close enough to avoid lingering near-zero values.
        if (_overlay.CurrentIntensity < 0.005f)
            _overlay.CurrentIntensity = 0f;
    }

    private void OnPlayerAttached(EntityUid uid, BloodstreamComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, BloodstreamComponent component, LocalPlayerDetachedEvent args)
    {
        _overlay.CurrentIntensity = 0f;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnBloodstreamInit(EntityUid uid, BloodstreamComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnBloodstreamShutdown(EntityUid uid, BloodstreamComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlay.CurrentIntensity = 0f;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}

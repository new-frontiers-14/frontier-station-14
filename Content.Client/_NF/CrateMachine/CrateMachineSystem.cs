using Content.Shared._NF.CrateMachine;
using Content.Shared._NF.CrateMachine.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Audio.Systems;

namespace Content.Client._NF.CrateMachine;

public sealed class CrateMachineSystem : SharedCrateMachineSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private const string AnimationKey = "crate_machine_animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrateMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CrateMachineComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentInit(EntityUid uid, CrateMachineComponent crateMachine, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        UpdateState(uid, crateMachine, sprite, appearance);
    }

    /// <summary>
    /// Update visuals and tick animation
    /// </summary>
    private void UpdateState(EntityUid uid, CrateMachineComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        // Get the current animation state, and default to Closed.
        if (!_appearanceSystem.TryGetData(uid, CrateMachineVisuals.VisualState, out CrateMachineVisualState state, appearance))
        {
            state = CrateMachineVisualState.Closed;
        }

        sprite.LayerSetVisible(CrateMachineVisualLayers.Base, true);
        sprite.LayerSetVisible(CrateMachineVisualLayers.Closed, state == CrateMachineVisualState.Closed);
        sprite.LayerSetVisible(CrateMachineVisualLayers.Opening, state == CrateMachineVisualState.Opening);
        sprite.LayerSetVisible(CrateMachineVisualLayers.Closing, state == CrateMachineVisualState.Closing);
        sprite.LayerSetVisible(CrateMachineVisualLayers.Open, state == CrateMachineVisualState.Open);

        // If the animation is already running, don't start it again.
        if (_animationSystem.HasRunningAnimation(uid, AnimationKey))
            return;
        // No need to animate open or closed state.
        if (state is CrateMachineVisualState.Open or CrateMachineVisualState.Closed)
            return;

        var layer = state switch
        {
            CrateMachineVisualState.Opening => CrateMachineVisualLayers.Opening,
            CrateMachineVisualState.Closing => CrateMachineVisualLayers.Closing,
            _ => CrateMachineVisualLayers.Closed
        };
        var spriteState = state switch
        {
            CrateMachineVisualState.Opening => component.OpeningSpriteState,
            CrateMachineVisualState.Closing => component.ClosingSpriteState,
            _ => component.ClosedSpriteState
        };
        var animationState = sprite.LayerMapTryGet(layer, out var layerIndex)
            ? sprite.LayerGetState(layerIndex)
            : new RSI.StateId(spriteState);

        var length = state switch
        {
            CrateMachineVisualState.Opening => component.OpeningTime,
            CrateMachineVisualState.Closing => component.ClosingTime,
            _ => 0,
        };
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = layer,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(animationState, 0) },
                },
            },
        };

        // Add sound to the animation
        var sound = state switch
        {
            CrateMachineVisualState.Opening => component.OpeningSound,
            CrateMachineVisualState.Closing => component.ClosingSound,
            _ => null,
        };
        if (sound != null)
        {
            animation.AnimationTracks.Add(
                new AnimationTrackPlaySound
                {
                    KeyFrames = { new AnimationTrackPlaySound.KeyFrame(_audioSystem.GetSound(sound), 0f) },
                }
            );
        }

        _animationSystem.Play(uid, animation, AnimationKey);
    }

    private void OnAppearanceChange(EntityUid uid, CrateMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(uid, component, args.Sprite, args.Component);
    }
}

public enum CrateMachineVisualLayers : byte
{
    Base,
    Opening,
    Open,
    Closing,
    Closed,
}

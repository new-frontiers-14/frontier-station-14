using Content.Shared._NF.Market;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Audio.Systems;
using static Content.Shared._NF.Market.Components.CrateMachineComponent;
using CrateMachineComponent = Content.Shared._NF.Market.Components.CrateMachineComponent;

namespace Content.Client._NF.Market.Systems;

public sealed class MarketSystem : SharedMarketSystem
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
        if (!_appearanceSystem.TryGetData<CrateMachineVisualState>(uid, CrateMachineVisuals.VisualState, out var state, appearance))
        {
            return;
        }

        sprite.LayerSetVisible(CrateMachineVisualLayers.Base, true);
        sprite.LayerSetVisible(CrateMachineVisualLayers.Closed, state == CrateMachineVisualState.Closed);
        sprite.LayerSetVisible(CrateMachineVisualLayers.Open, state == CrateMachineVisualState.Open);
        sprite.LayerSetVisible(CrateMachineVisualLayers.BaseDoors, state is CrateMachineVisualState.Opening or CrateMachineVisualState.Closing);
        sprite.LayerSetVisible(CrateMachineVisualLayers.Crate, state is CrateMachineVisualState.Opening);

        if (state == CrateMachineVisualState.Opening && !_animationSystem.HasRunningAnimation(uid, AnimationKey))
        {
            var openingState = new RSI.StateId(component.OpeningSpriteState);
            var crateState = new RSI.StateId(component.CrateSpriteState);
            // Setup the opening animation to play
            var anim = new Animation
            {
                Length = TimeSpan.FromSeconds(component.OpeningTime),
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = CrateMachineVisualLayers.BaseDoors,
                        KeyFrames =
                        {
                            // Play the opening & crate animation making the crate appear going up.
                            new AnimationTrackSpriteFlick.KeyFrame(openingState, 0),
                            new AnimationTrackSpriteFlick.KeyFrame(crateState, 0),
                        }
                    },
                }
            };

            if (component.OpeningSound != null)
            {
                anim.AnimationTracks.Add(
                    new AnimationTrackPlaySound
                    {
                        KeyFrames =
                        {
                            new AnimationTrackPlaySound.KeyFrame(_audioSystem.GetSound(component.OpeningSound), 0)
                        }
                    }
                );
            }

            _animationSystem.Play(uid, anim, AnimationKey);
        }
        else if (state == CrateMachineVisualState.Closing && !_animationSystem.HasRunningAnimation(uid, AnimationKey))
        {
            var closingState = new RSI.StateId(component.ClosingSpriteState);
            // Setup the opening animation to play
            var anim = new Animation
            {
                Length = TimeSpan.FromSeconds(component.ClosingTime),
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = CrateMachineVisualLayers.BaseDoors,
                        KeyFrames =
                        {
                            // Play the flush animation
                            new AnimationTrackSpriteFlick.KeyFrame(closingState, 0),
                        }
                    },
                }
            };

            if (component.ClosingSound != null)
            {
                anim.AnimationTracks.Add(
                    new AnimationTrackPlaySound
                    {
                        KeyFrames =
                        {
                            new AnimationTrackPlaySound.KeyFrame(_audioSystem.GetSound(component.ClosingSound), 0)
                        }
                    }
                );
            }

            _animationSystem.Play(uid, anim, AnimationKey);
        }
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
    BaseDoors,
    Open,
    Closed,
    Crate
}

using Content.Shared._NF.Pirate;
using Content.Shared._NF.Pirate.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._NF.Pirate.Systems;

public sealed partial class PirateSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private static readonly Animation PirateTelepadBeamAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.5),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = PirateTelepadLayers.Beam,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("beam"), 0f)
                }
            }
        }
    };

    private static readonly Animation PirateTelepadIdleAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.8),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = PirateTelepadLayers.Beam,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("idle"), 0f)
                }
            }
        }
    };

    // TODO: fluent entries for this
    private const string TelepadBeamKey = "pirate-telepad-beam";
    private const string TelepadIdleKey = "pirate-telepad-idle";

    private void InitializePirateTelepad()
    {
        SubscribeLocalEvent<PirateTelepadComponent, AppearanceChangeEvent>(OnPirateAppChange);
        SubscribeLocalEvent<PirateTelepadComponent, AnimationCompletedEvent>(OnPirateAnimComplete);
    }

    private void OnPirateAppChange(EntityUid uid, PirateTelepadComponent component, ref AppearanceChangeEvent args)
    {
        OnChangeData(uid, args.Sprite);
    }

    private void OnPirateAnimComplete(EntityUid uid, PirateTelepadComponent component, AnimationCompletedEvent args)
    {
        OnChangeData(uid);
    }

    private void OnChangeData(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        _appearance.TryGetData<PirateTelepadState?>(uid, PirateTelepadVisuals.State, out var state);
        AnimationPlayerComponent? player = null;

        switch (state)
        {
            case PirateTelepadState.Teleporting:
                if (_player.HasRunningAnimation(uid, TelepadBeamKey))
                    return;
                _player.Stop(uid, player, TelepadIdleKey);
                _player.Play(uid, player, PirateTelepadBeamAnimation, TelepadBeamKey);
                break;
            case PirateTelepadState.Unpowered:
                sprite.LayerSetVisible(PirateTelepadLayers.Beam, false);
                _player.Stop(uid, player, TelepadBeamKey);
                _player.Stop(uid, player, TelepadIdleKey);
                break;
            default:
                sprite.LayerSetVisible(PirateTelepadLayers.Beam, true);

                if (_player.HasRunningAnimation(uid, player, TelepadIdleKey) ||
                    _player.HasRunningAnimation(uid, player, TelepadBeamKey))
                    return;

                _player.Play(uid, player, PirateTelepadIdleAnimation, TelepadIdleKey);
                break;
        }
    }

    [UsedImplicitly]
    private enum PirateTelepadLayers : byte
    {
        Base = 0,
        Beam = 1,
    }
}

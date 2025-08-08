using Content.Shared._NF.Cargo.Components;
using Content.Shared.Cargo;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._NF.Cargo.Systems;

public sealed partial class NFCargoSystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private static readonly Animation CargoTelepadBeamAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.5),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = NFCargoTelepadLayers.Beam,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("beam"), 0f)
                }
            }
        }
    };

    private static readonly Animation CargoTelepadIdleAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.8),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = NFCargoTelepadLayers.Beam,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("idle"), 0f)
                }
            }
        }
    };

    private const string TelepadBeamKey = "cargo-telepad-beam";
    private const string TelepadIdleKey = "cargo-telepad-idle";

    private void InitializeCargoTelepad()
    {
        SubscribeLocalEvent<NFCargoTelepadComponent, AppearanceChangeEvent>(OnCargoAppChange);
        SubscribeLocalEvent<NFCargoTelepadComponent, AnimationCompletedEvent>(OnCargoAnimComplete);
    }

    private void OnCargoAppChange(Entity<NFCargoTelepadComponent> ent, ref AppearanceChangeEvent args)
    {
        OnChangeData(ent, args.Sprite);
    }

    private void OnCargoAnimComplete(Entity<NFCargoTelepadComponent> ent, ref AnimationCompletedEvent args)
    {
        OnChangeData(ent);
    }

    private void OnChangeData(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        _appearance.TryGetData<CargoTelepadState?>(uid, CargoTelepadVisuals.State, out var state);

        switch (state)
        {
            case CargoTelepadState.Teleporting:
                _player.Stop((uid, player), TelepadIdleKey);
                if (!_player.HasRunningAnimation(uid, TelepadBeamKey))
                    _player.Play((uid, player), CargoTelepadBeamAnimation, TelepadBeamKey);
                break;
            case CargoTelepadState.Unpowered:
                sprite.LayerSetVisible(NFCargoTelepadLayers.Beam, false);
                _player.Stop(uid, player, TelepadBeamKey);
                _player.Stop(uid, player, TelepadIdleKey);
                break;
            default:
                sprite.LayerSetVisible(NFCargoTelepadLayers.Beam, true);

                if (_player.HasRunningAnimation(uid, player, TelepadIdleKey) ||
                    _player.HasRunningAnimation(uid, player, TelepadBeamKey))
                    return;

                _player.Play((uid, player), CargoTelepadIdleAnimation, TelepadIdleKey);
                break;
        }
    }

    [UsedImplicitly]
    private enum NFCargoTelepadLayers : byte
    {
        Base = 0,
        Beam = 1,
    }
}

using Robust.Shared.Map;
using Robust.Client.GameObjects;
using Content.Shared.Repairable;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Client.Popups;
using Content.Client.Examine;
using Robust.Client.Animations;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;

namespace Content.Client._FarHorizons.Power.Generation.FissionGenerator;

// Ported and modified from goonstation by Jhrushbe.
// CC-BY-NC-SA-3.0
// https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/turbine.dm

public sealed class TurbineSystem : SharedTurbineSystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly float _threshold = 1f;
    private float _accumulator = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurbineComponent, ClientExaminedEvent>(TurbineExamined);

        SubscribeLocalEvent<TurbineComponent, AnimationCompletedEvent>(OnAnimationCompleted);

        SubscribeLocalEvent<TurbineComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<TurbineComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
    }

    protected override void OnRepairTurbineFinished(EntityUid uid, TurbineComponent comp, ref RepairDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _popupSystem.PopupClient(Loc.GetString("turbine-repair", ("target", uid), ("tool", args.Used!)), uid, args.User);
    }

    private void TurbineExamined(EntityUid uid, TurbineComponent comp, ClientExaminedEvent args) => Spawn(comp.ArrowPrototype, new EntityCoordinates(uid, 0, 0));

    #region Animation
    private void OnAnimationCompleted(EntityUid uid, TurbineComponent comp, ref AnimationCompletedEvent args) => PlayAnimation(uid, comp);

    public override void FrameUpdate(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator >= _threshold)
        {
            AccUpdate();
            _accumulator = 0;
        }
    }

    private void AccUpdate()
    {
        var query = EntityQueryEnumerator<TurbineComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            // Makes sure the anim doesn't get stuck at low RPM
            PlayAnimation(uid, component);
        }
    }

    private void PlayAnimation(EntityUid uid, TurbineComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !_sprite.TryGetLayer((uid,sprite), TurbineVisualLayers.TurbineSpeed, out var layer, false))
            return;

        var state = "speedanim";
        if (comp.RPM < 1)
        {
            _animationPlayer.Stop(uid, state);
            _sprite.LayerSetRsiState(layer, "turbine");
            comp.AnimRPM = -comp.BestRPM; // Primes it to start the instant it's spinning again
            return;
        }

        if (Math.Abs(comp.RPM - comp.AnimRPM) > comp.BestRPM * 0.1)
            _animationPlayer.Stop(uid, state); // Current anim is stale, time for a new one

        if (_animationPlayer.HasRunningAnimation(uid, state))
            return;

        comp.AnimRPM = comp.RPM;
        var layerKey = TurbineVisualLayers.TurbineSpeed;
        var time = 0.5f * comp.BestRPM / comp.RPM;
        var timestep = time / 12;
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(time),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = layerKey,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_00", 0),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_01", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_02", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_03", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_04", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_05", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_06", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_07", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_08", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_09", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_10", timestep),
                        new AnimationTrackSpriteFlick.KeyFrame("turbinerun_11", timestep)
                    }
                }
            }
        };
        _sprite.LayerSetVisible(layer, true);
        _animationPlayer.Play(uid, animation, state);
    }
    #endregion

    private void OnEjectAttempt(EntityUid uid, TurbineComponent comp, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (comp.RPM < 1)
            return;

        args.Cancelled = true;
    }

    private void OnInsertAttempt(EntityUid uid, TurbineComponent comp, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (comp.RPM < 1)
            return;

        args.Cancelled = true;
    }
}

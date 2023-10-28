using System.Numerics;
// using Content.Server.Mind;
// using Content.Server.Mind.Components;
using Content.Server._Park.Species.Shadowkin.Events;
using Content.Server._Park.Species.Shadowkin.Components;
using Content.Shared.Actions;
using Content.Shared.Magic.Events;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared._Park.Species.Shadowkin.Components;
using Content.Shared._Park.Species.Shadowkin.Events;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Park.Species.Shadowkin.Systems;

public sealed class ShadowkinSystem : EntitySystem
{
    [Dependency] private readonly ShadowkinPowerSystem _power = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ShadowkinComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShadowkinComponent, ComponentShutdown>(OnShutdown);
    }


    private void OnExamine(EntityUid uid, ShadowkinComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var powerType = _power.GetLevelName(component.PowerLevel);

        // Show exact values for yourself
        if (args.Examined == args.Examiner)
        {
            args.PushMarkup(Loc.GetString("shadowkin-power-examined-self",
                ("power", (int) component.PowerLevel),
                ("powerMax", component.PowerLevelMax),
                ("powerType", powerType)
            ));
        }
        // Show general values for others
        else
        {
            args.PushMarkup(Loc.GetString("shadowkin-power-examined-other",
                ("target", Identity.Entity(uid, _entity)),
                ("powerType", powerType)
            ));
        }
    }

    private void OnInit(EntityUid uid, ShadowkinComponent component, ComponentInit args)
    {
        if (component.PowerLevel <= ShadowkinComponent.PowerThresholds[ShadowkinPowerThreshold.Min] + 1f)
            _power.SetPowerLevel(uid, ShadowkinComponent.PowerThresholds[ShadowkinPowerThreshold.Good]);

        var max = _random.NextFloat(component.MaxedPowerRateMin, component.MaxedPowerRateMax);
        component.MaxedPowerAccumulator = max;
        component.MaxedPowerRoof = max;

        var min = _random.NextFloat(component.MinPowerMin, component.MinPowerMax);
        component.MinPowerAccumulator = min;
        component.MinPowerRoof = min;

        _power.UpdateAlert(uid, true, component.PowerLevel);
    }

    private void OnShutdown(EntityUid uid, ShadowkinComponent component, ComponentShutdown args)
    {
        _power.UpdateAlert(uid, false);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = _entity.EntityQueryEnumerator<ShadowkinComponent>();

        // Update power level for all shadowkin
        while (query.MoveNext(out var uid, out var shadowkin))
        {
            // Ensure dead or critical shadowkin aren't swapped, skip them
            if (_mobState.IsDead(uid) ||
                _mobState.IsCritical(uid))
            {
                _entity.RemoveComponent<ShadowkinDarkSwappedComponent>(uid);
                continue;
            }

            // Don't update things for ssd shadowkin
            // if (!_entity.System<MindSystem>().TryGetMind(uid, out var mind) ||
            //     mind.Session == null)
            //     continue;


            var oldPowerLevel = _power.GetLevelName(shadowkin.PowerLevel);
            _power.TryUpdatePowerLevel(uid, frameTime);

            if (oldPowerLevel != _power.GetLevelName(shadowkin.PowerLevel))
            {
                _power.TryBlackeye(uid);
                Dirty(shadowkin);
            }
            // I can't figure out how to get this to go to the 100% filled state in the above if statement 😢
            _power.UpdateAlert(uid, true, shadowkin.PowerLevel);


            // Don't randomly activate abilities if handcuffed
            // TODO: Something like the Psionic Headcage to disable powers for Shadowkin
            if (_entity.HasComponent<HandcuffComponent>(uid))
                continue;

            #region MaxPower
            // Check if they're at max power
            if (shadowkin.PowerLevel >= ShadowkinComponent.PowerThresholds[ShadowkinPowerThreshold.Max])
            {
                // If so, start the timer
                shadowkin.MaxedPowerAccumulator -= frameTime;

                // If the time's up, do things
                if (shadowkin.MaxedPowerAccumulator <= 0f)
                {
                    // Randomize the timer
                    var next = _random.NextFloat(shadowkin.MaxedPowerRateMin, shadowkin.MaxedPowerRateMax);
                    shadowkin.MaxedPowerRoof = next;
                    shadowkin.MaxedPowerAccumulator = next;

                    var chance = _random.Next(7);

                    if (chance <= 2)
                    {
                        ForceDarkSwap(uid, shadowkin);
                    }
                    else if (chance <= 7)
                    {
                        ForceTeleport(uid, shadowkin);
                    }
                }
            }
            else
            {
                // Slowly regenerate if not maxed
                shadowkin.MaxedPowerAccumulator += frameTime / 5f;
                shadowkin.MaxedPowerAccumulator = Math.Clamp(shadowkin.MaxedPowerAccumulator, 0f, shadowkin.MaxedPowerRoof);
            }
            #endregion

            #region MinPower
            // Check if they're at the average of the Tired and Okay thresholds
            // Just Tired is too little, and Okay is too much, get the average
            if (shadowkin.PowerLevel <=
                (
                    ShadowkinComponent.PowerThresholds[ShadowkinPowerThreshold.Tired] +
                    ShadowkinComponent.PowerThresholds[ShadowkinPowerThreshold.Okay]
                ) / 2f &&
                // Don't sleep if asleep
                !_entity.HasComponent<SleepingComponent>(uid)
            )
            {
                // If so, start the timer
                shadowkin.MinPowerAccumulator -= frameTime;

                // If the timer is up, force rest
                if (shadowkin.MinPowerAccumulator <= 0f)
                {
                    // Random new timer
                    var next = _random.NextFloat(shadowkin.MinPowerMin, shadowkin.MinPowerMax);
                    shadowkin.MinPowerRoof = next;
                    shadowkin.MinPowerAccumulator = next;

                    // Send event to rest
                    RaiseLocalEvent(uid, new ShadowkinRestEvent { Performer = uid });
                }
            }
            else
            {
                // Slowly regenerate if not tired
                shadowkin.MinPowerAccumulator += frameTime / 5f;
                shadowkin.MinPowerAccumulator = Math.Clamp(shadowkin.MinPowerAccumulator, 0f, shadowkin.MinPowerRoof);
            }
            #endregion
        }
    }

    private void ForceDarkSwap(EntityUid uid, ShadowkinComponent component)
    {
        // Add/Remove the component, which should handle the rest
        if (_entity.HasComponent<ShadowkinDarkSwappedComponent>(uid))
            _entity.RemoveComponent<ShadowkinDarkSwappedComponent>(uid);
        else
            _entity.AddComponent<ShadowkinDarkSwappedComponent>(uid);
    }

    private void ForceTeleport(EntityUid uid, ShadowkinComponent component)
    {
        // Create the event we'll later raise, and set it to our Shadowkin.
        var args = new ShadowkinTeleportEvent { Performer = uid };

        // Pick a random location on the map until we find one that can be reached.
        var coords = Transform(uid).Coordinates;
        EntityCoordinates? target = null;

        // It'll iterate up to 8 times, shrinking in distance each time, and if it doesn't find a valid location, it'll return.
        for (var i = 8; i != 0; i--)
        {
            var angle = Angle.FromDegrees(_random.Next(360));
            var offset = new Vector2((float) (i * Math.Cos(angle)), (float) (i * Math.Sin(angle)));

            target = coords.Offset(offset);

            if (_interaction.InRangeUnobstructed(uid, target.Value, 0,
                    CollisionGroup.MobMask | CollisionGroup.MobLayer))
                break;

            target = null;
        }

        // If we didn't find a valid location, return.
        if (target == null)
            return;

        args.Target = target.Value;

        // Raise the event to teleport the Shadowkin.
        RaiseLocalEvent(uid, args);
    }
}

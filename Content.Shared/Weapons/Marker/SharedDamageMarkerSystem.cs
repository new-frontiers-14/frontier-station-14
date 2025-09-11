using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared.Mobs; // Frontier
using Content.Shared.Mobs.Components; // Frontier
using Content.Shared.Mobs.Systems; // Frontier
using Content.Shared._NF.Weapons.Components; // Frontier

namespace Content.Shared.Weapons.Marker;

public abstract class SharedDamageMarkerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!; // Frontier
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, StartCollideEvent>(OnMarkerCollide);
        SubscribeLocalEvent<DamageMarkerComponent, AttackedEvent>(OnMarkerAttacked);

        SubscribeLocalEvent<NFMarkerCounterComponent, MobStateChangedEvent>(OnMobStateChange); // Frontier - remove marker counter on revive
    }

    private void OnMarkerAttacked(EntityUid uid, DamageMarkerComponent component, AttackedEvent args)
    {
        if (component.Marker != args.Used)
            return;

        args.BonusDamage += component.Damage;
        RemCompDeferred<DamageMarkerComponent>(uid);
        _audio.PlayPredicted(component.Sound, uid, args.User);

        if (TryComp<LeechOnMarkerComponent>(args.Used, out var leech))
        {
            // Frontier - limit crusher whacks on dead mobs
            // Always leech on living targets
            // You're guaranteed a number of leech hits dead or alive
            // Can be tuned for balancing purposes
            // Only stick a marker counter onto living mobs, so you can't leech from a corpse you never leeched from while alive
            var isAlive = TryComp<MobStateComponent>(uid, out var state) && !_mobStateSystem.IsDead(uid, state);
            // Can always leech from living mobs
            var weCanLeech = isAlive;
            // We give the marker counter to living mobs only
            if (!TryComp<NFMarkerCounterComponent>(uid, out var markerCounter) && isAlive)
            {
                EnsureComp<NFMarkerCounterComponent>(uid, out markerCounter);
                markerCounter.WhacksRemaining = leech.NumGuaranteedLeechHits;
            }

            // If the mob has a marker counter with remaining charges, we can leech even if dead
            // Deduct a leech charge in either case
            if (markerCounter != null && markerCounter.WhacksRemaining > 0)
            {
                markerCounter.WhacksRemaining = Math.Max(markerCounter.WhacksRemaining - 1, 0);
                weCanLeech = true;
            }
            if (weCanLeech)
                _damageable.TryChangeDamage(args.User, leech.Leech, true, false, origin: args.Used);
            // End Frontier
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamageMarkerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime > _timing.CurTime)
                continue;

            RemCompDeferred<DamageMarkerComponent>(uid);
        }
    }

    private void OnMarkerCollide(EntityUid uid, DamageMarkerOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            component.Amount <= 0 ||
            _whitelistSystem.IsWhitelistFail(component.Whitelist, args.OtherEntity) ||
            !TryComp<ProjectileComponent>(uid, out var projectile) ||
            projectile.Weapon == null)
        {
            return;
        }

        // Markers are exclusive, deal with it.
        var marker = EnsureComp<DamageMarkerComponent>(args.OtherEntity);
        marker.Damage = new DamageSpecifier(component.Damage);
        marker.Marker = projectile.Weapon.Value;
        marker.EndTime = _timing.CurTime + component.Duration;
        component.Amount--;
        Dirty(args.OtherEntity, marker);

        if (_netManager.IsServer)
        {
            if (component.Amount <= 0)
            {
                QueueDel(uid);
            }
            else
            {
                Dirty(uid, component);
            }
        }
    }

    // Frontier - remove the marker counter if mob is revived
    private void OnMobStateChange(EntityUid uid, NFMarkerCounterComponent component, ref MobStateChangedEvent args)
    {
        // Revival (FROM Dead TO anywhere else)
        if (args.OldMobState == MobState.Dead)
        {
            RemCompDeferred<NFMarkerCounterComponent>(uid);
        }
    }
    // End Frontier
}

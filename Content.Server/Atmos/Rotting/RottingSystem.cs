using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Server.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Shared.Cuffs.Components;

namespace Content.Server.Atmos.Rotting;

public sealed class RottingSystem : SharedRottingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PerishableComponent, MapInitEvent>(OnPerishableMapInit);
        SubscribeLocalEvent<PerishableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PerishableComponent, ExaminedEvent>(OnPerishableExamined);

        SubscribeLocalEvent<RottingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RottingComponent, MobStateChangedEvent>(OnRottingMobStateChanged);
        SubscribeLocalEvent<RottingComponent, BeingGibbedEvent>(OnGibbed);
        SubscribeLocalEvent<RottingComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<TemperatureComponent, IsRottingEvent>(OnTempIsRotting);
    }

    private void OnPerishableMapInit(EntityUid uid, PerishableComponent component, MapInitEvent args)
    {
        component.RotNextUpdate = _timing.CurTime + component.PerishUpdateRate;
    }

    private void OnMobStateChanged(EntityUid uid, PerishableComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead && args.OldMobState != MobState.Dead)
            return;

        if (HasComp<RottingComponent>(uid))
            return;

        component.RotAccumulator = TimeSpan.Zero;
        component.RotNextUpdate = _timing.CurTime + component.PerishUpdateRate;
    }

    private void OnShutdown(EntityUid uid, RottingComponent component, ComponentShutdown args)
    {
        if (TryComp<PerishableComponent>(uid, out var perishable))
        {
            perishable.RotNextUpdate = TimeSpan.Zero;
        }
    }

    private void OnRottingMobStateChanged(EntityUid uid, RottingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            return;
        RemCompDeferred(uid, component);
    }

    public bool IsRotProgressing(EntityUid uid, PerishableComponent? perishable)
    {
        // things don't perish by default.
        if (!Resolve(uid, ref perishable, false))
            return false;

        // only dead things or inanimate objects can rot
        if (TryComp<MobStateComponent>(uid, out var mobState) && !_mobState.IsDead(uid, mobState))
            return false;

        if (_container.TryGetOuterContainer(uid, Transform(uid), out var container) &&
            HasComp<AntiRottingContainerComponent>(container.Owner))
        {
            return false;
        }

        if (TryComp<CuffableComponent>(uid, out var cuffed) && cuffed.CuffedHandCount > 0)
        {
            if (TryComp<HandcuffComponent>(cuffed.LastAddedCuffs, out var cuffcomp))
                {
                    if (cuffcomp.NoRot)
                    {
                        return false;
                    }
                }
        }

        var ev = new IsRottingEvent();
        RaiseLocalEvent(uid, ref ev);

        return !ev.Handled;
    }

    public bool IsRotten(EntityUid uid, RottingComponent? rotting = null)
    {
        return Resolve(uid, ref rotting, false);
    }

    private void OnGibbed(EntityUid uid, RottingComponent component, BeingGibbedEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        if (!TryComp<PerishableComponent>(uid, out var perishable))
            return;

        var molsToDump = perishable.MolsPerSecondPerUnitMass * physics.FixturesMass * (float) component.TotalRotTime.TotalSeconds;
        var tileMix = _atmosphere.GetTileMixture(uid, excite: true);
        tileMix?.AdjustMoles(Gas.Ammonia, molsToDump);
    }

    private void OnPerishableExamined(Entity<PerishableComponent> perishable, ref ExaminedEvent args)
    {
        int stage = PerishStage(perishable, MaxStages);
        if (stage < 1 || stage > MaxStages)
        {
            // We dont push an examined string if it hasen't started "perishing" or it's already rotting
            return;
        }

        var isMob = HasComp<MobStateComponent>(perishable);
        var description = "perishable-" + stage + (!isMob ? "-nonmob" : string.Empty);
        args.PushMarkup(Loc.GetString(description, ("target", Identity.Entity(perishable, EntityManager))));
    }

    /// <summary>
    /// Return an integer from 0 to maxStage representing how close to rotting an entity is. Used to
    /// generate examine messages for items that are starting to rot.
    /// </summary>
    public int PerishStage(Entity<PerishableComponent> perishable, int maxStages)
    {
        if (perishable.Comp.RotAfter.TotalSeconds == 0 || perishable.Comp.RotAccumulator.TotalSeconds == 0)
            return 0;
        return (int)(1 + maxStages * perishable.Comp.RotAccumulator.TotalSeconds / perishable.Comp.RotAfter.TotalSeconds);
    }

    private void OnRejuvenate(EntityUid uid, RottingComponent component, RejuvenateEvent args)
    {
        RemCompDeferred<RottingComponent>(uid);
    }

    private void OnTempIsRotting(EntityUid uid, TemperatureComponent component, ref IsRottingEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = component.CurrentTemperature < Atmospherics.T0C + 0.85f;
    }


    public void ReduceAccumulator(EntityUid uid, TimeSpan time)
    {
        if (!TryComp<PerishableComponent>(uid, out var perishable))
            return;

        if (!TryComp<RottingComponent>(uid, out var rotting))
        {
            perishable.RotAccumulator -= time;
            return;
        }
        var total = (rotting.TotalRotTime + perishable.RotAccumulator) - time;

        if (total < perishable.RotAfter)
        {
            RemCompDeferred(uid, rotting);
            perishable.RotAccumulator = total;
        }

        else
            rotting.TotalRotTime = total - perishable.RotAfter;
    }
    
    /// <summary>
    /// Is anything speeding up the decay?
    /// e.g. buried in a grave
    /// TODO: hot temperatures increase rot?
    /// </summary>
    /// <returns></returns>
    private float GetRotRate(EntityUid uid)
    {
        if (_container.TryGetContainingContainer(uid, out var container) &&
            TryComp<ProRottingContainerComponent>(container.Owner, out var rotContainer))
        {
            return rotContainer.DecayModifier;
        }

        return 1f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var perishQuery = EntityQueryEnumerator<PerishableComponent>();
        while (perishQuery.MoveNext(out var uid, out var perishable))
        {
            if (_timing.CurTime < perishable.RotNextUpdate)
                continue;
            perishable.RotNextUpdate += perishable.PerishUpdateRate;

            var stage = PerishStage((uid, perishable), MaxStages);
            if (stage != perishable.Stage)
            {
                perishable.Stage = stage;
                Dirty(uid, perishable);
            }

            if (IsRotten(uid) || !IsRotProgressing(uid, perishable))
                continue;

            perishable.RotAccumulator += perishable.PerishUpdateRate * GetRotRate(uid);
            if (perishable.RotAccumulator >= perishable.RotAfter)
            {
                var rot = AddComp<RottingComponent>(uid);
                rot.NextRotUpdate = _timing.CurTime + rot.RotUpdateRate;
            }
        }

        var rotQuery = EntityQueryEnumerator<RottingComponent, PerishableComponent, TransformComponent>();
        while (rotQuery.MoveNext(out var uid, out var rotting, out var perishable, out var xform))
        {
            if (_timing.CurTime < rotting.NextRotUpdate) // This is where it starts to get noticable on larger animals, no need to run every second
                continue;
            rotting.NextRotUpdate += rotting.RotUpdateRate;

            if (!IsRotProgressing(uid, perishable))
                continue;
            rotting.TotalRotTime += rotting.RotUpdateRate * GetRotRate(uid);

            if (rotting.DealDamage)
            {
                var damage = rotting.Damage * rotting.RotUpdateRate.TotalSeconds;
                _damageable.TryChangeDamage(uid, damage, true, false);
            }

            if (TryComp<RotIntoComponent>(uid, out var rotInto))
            {
                var stage = RotStage(uid, rotting, perishable);
                if (stage >= rotInto.Stage)
                {
                    Spawn(rotInto.Entity, xform.Coordinates);
                    QueueDel(uid);
                    continue;
                }
            }

            if (!TryComp<PhysicsComponent>(uid, out var physics))
                continue;
            // We need a way to get the mass of the mob alone without armor etc in the future
            // or just remove the mass mechanics altogether because they aren't good.
            var molRate = perishable.MolsPerSecondPerUnitMass * (float) rotting.RotUpdateRate.TotalSeconds;
            var tileMix = _atmosphere.GetTileMixture(uid, excite: true);
            tileMix?.AdjustMoles(Gas.Ammonia, molRate * physics.FixturesMass);
        }
    }
}

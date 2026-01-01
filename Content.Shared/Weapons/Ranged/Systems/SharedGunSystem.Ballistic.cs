using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;


    protected virtual void InitializeBallistic()
    {
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentInit>(OnBallisticInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, MapInitEvent>(OnBallisticMapInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, TakeAmmoEvent>(OnBallisticTakeAmmo);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetAmmoCountEvent>(OnBallisticAmmoCount);

        SubscribeLocalEvent<BallisticAmmoProviderComponent, ExaminedEvent>(OnBallisticExamine);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetVerbsEvent<Verb>>(OnBallisticVerb);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, InteractUsingEvent>(OnBallisticInteractUsing);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AfterInteractEvent>(OnBallisticAfterInteract);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AmmoFillDoAfterEvent>(OnBallisticAmmoFillDoAfter);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, UseInHandEvent>(OnBallisticUse);

        SubscribeLocalEvent<BallisticAmmoSelfRefillerComponent, MapInitEvent>(OnBallisticRefillerMapInit);
        SubscribeLocalEvent<BallisticAmmoSelfRefillerComponent, EmpPulseEvent>(OnRefillerEmpPulsed);
    }

    private void OnBallisticRefillerMapInit(Entity<BallisticAmmoSelfRefillerComponent> entity, ref MapInitEvent _)
    {
        entity.Comp.NextAutoRefill = Timing.CurTime + entity.Comp.AutoRefillRate;
    }

    private void OnBallisticUse(Entity<BallisticAmmoProviderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ManualCycle(ent, TransformSystem.GetMapCoordinates(ent), args.User);
        args.Handled = true;
    }

    private void OnBallisticInteractUsing(Entity<BallisticAmmoProviderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryBallisticInsert(ent, args.Used, args.User))
            args.Handled = true;
    }

    private void OnBallisticAfterInteract(EntityUid uid, BallisticAmmoProviderComponent component, AfterInteractEvent args)
    {
        if (args.Handled ||
            !component.MayTransfer ||
            !Timing.IsFirstTimePredicted ||
            args.Target == null ||
            args.Used == args.Target ||
            Deleted(args.Target))
        {
            return;
        }

        // Frontier: better revolver reloading
        // Ensure the target of interaction has a valid component.
        var validComponent = false;
        TimeSpan fillDelay = component.FillDelay; // Default value should not be used.
        if (TryComp<BallisticAmmoProviderComponent>(args.Target, out var ballisticComponent) && ballisticComponent.Whitelist is not null)
        {
            validComponent = true;
            fillDelay = ballisticComponent.FillDelay;
        }
        else if (TryComp<RevolverAmmoProviderComponent>(args.Target, out var revolverComponent) && revolverComponent.Whitelist is not null)
        {
            validComponent = true;
            fillDelay = revolverComponent.FillDelay;
        }

        if (validComponent) // End Frontier
        {
            args.Handled = true;

            // Continuous loading
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, fillDelay, new AmmoFillDoAfterEvent(), used: uid, target: args.Target, eventTarget: uid) // Frontier: component.FillDelay<fillDelay
            {
                BreakOnMove = true,
                BreakOnDamage = false,
                NeedHand = true
            });
        }
    }

    private void OnBallisticAmmoFillDoAfter(EntityUid uid, BallisticAmmoProviderComponent component, AmmoFillDoAfterEvent args)
    {
        if (Deleted(args.Target)) // Frontier: deferred component & whitelist check
            return;

        // Frontier: Better revolver reloading
        BallisticAmmoProviderComponent? ballisticTarget;
        RevolverAmmoProviderComponent? revolverTarget = null;
        if (!TryComp(args.Target, out ballisticTarget) && !TryComp(args.Target, out revolverTarget))
        {
            return;
        }
        if ((ballisticTarget is null || ballisticTarget.Whitelist is null) &&
            (revolverTarget is null || revolverTarget.Whitelist is null))
        {
            // No supported component type with valid whitelist.
            return;
        }

        //Check capacity
        if (ballisticTarget is not null && GetBallisticShots(ballisticTarget) >= ballisticTarget.Capacity ||
            revolverTarget is not null && GetRevolverCount(revolverTarget) >= revolverTarget.Capacity)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-target-full",
                    ("entity", args.Target)),
                args.Target,
                args.User);
            return;
        }
        // End Frontier

        if (component.Entities.Count + component.UnspawnedCount == 0)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-empty",
                    ("entity", uid)),
                uid,
                args.User);
            return;
        }

        void SimulateInsertAmmo(EntityUid ammo, EntityUid ammoProvider, EntityCoordinates coordinates)
        {
            // We call SharedInteractionSystem to raise contact events. Checks are already done by this point.
            _interaction.InteractUsing(args.User, ammo, ammoProvider, coordinates, checkCanInteract: false, checkCanUse: false);
        }

        List<(EntityUid? Entity, IShootable Shootable)> ammo = new();
        var evTakeAmmo = new TakeAmmoEvent(1, ammo, Transform(uid).Coordinates, args.User);
        RaiseLocalEvent(uid, evTakeAmmo);

        bool validAmmoType = true; // Frontier: do not repeat reload attempts with invalid ammo.

        foreach (var (ent, _) in ammo)
        {
            if (ent == null)
                continue;

            if (ballisticTarget is not null && _whitelistSystem.IsWhitelistFailOrNull(ballisticTarget?.Whitelist, ent.Value) || // Frontier: better revolver reloading
                revolverTarget is not null && _whitelistSystem.IsWhitelistFailOrNull(revolverTarget?.Whitelist, ent.Value)) // Frontier: better revolver reloading
            {
                Popup(
                    Loc.GetString("gun-ballistic-transfer-invalid",
                        ("ammoEntity", ent.Value),
                        ("targetEntity", args.Target.Value)),
                    uid,
                    args.User);

                SimulateInsertAmmo(ent.Value, uid, Transform(uid).Coordinates);

                validAmmoType = false; // Frontier: do not retry reloading if the ammo type is different.
            }
            else
            {
                // play sound to be cool
                Audio.PlayPredicted(component.SoundInsert, uid, args.User);
                SimulateInsertAmmo(ent.Value, args.Target.Value, Transform(args.Target.Value).Coordinates);
            }

            if (IsClientSide(ent.Value))
                Del(ent.Value);
        }

        // repeat if there is more space in the target and more ammo to fill it
        // Frontier: better revolver reloading
        var moreSpace = false;
        if (ballisticTarget is not null)
            moreSpace = GetBallisticShots(ballisticTarget) < ballisticTarget.Capacity;
        else if (revolverTarget is not null)
            moreSpace = GetRevolverCount(revolverTarget) < revolverTarget.Capacity;
        // End Frontier
        var moreAmmo = component.Entities.Count + component.UnspawnedCount > 0;
        args.Repeat = moreSpace && moreAmmo && validAmmoType; // Frontier: do not repeat reload attempts with invalid ammo.
    }

    private void OnBallisticVerb(EntityUid uid, BallisticAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !component.Cycleable)
            return;

        if (component.Cycleable)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("gun-ballistic-cycle"),
                Disabled = GetBallisticShots(component) == 0,
                Act = () => ManualCycle((uid, component), TransformSystem.GetMapCoordinates(uid), args.User),
            });

        }
    }

    private void OnBallisticExamine(Entity<BallisticAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", GetBallisticShots(ent.Comp))));
    }

    private void ManualCycle(Entity<BallisticAmmoProviderComponent> ent, MapCoordinates coordinates, EntityUid? user = null, GunComponent? gunComp = null)
    {
        if (!ent.Comp.Cycleable)
            return;

        // Reset shotting for cycling
        if (Resolve(ent, ref gunComp, false) &&
            gunComp is { FireRateModified: > 0f } &&
            !Paused(ent))
        {
            gunComp.NextFire = Timing.CurTime + TimeSpan.FromSeconds(1 / gunComp.FireRateModified);
            DirtyField(ent, gunComp, nameof(GunComponent.NextFire));
        }

        Audio.PlayPredicted(ent.Comp.SoundRack, ent, user);

        var shots = GetBallisticShots(ent.Comp);
        Cycle(ent, coordinates);

        var text = Loc.GetString(shots == 0 ? "gun-ballistic-cycled-empty" : "gun-ballistic-cycled");

        Popup(text, ent, user);
        UpdateBallisticAppearance(ent);
        UpdateAmmoCount(ent);
    }

    protected abstract void Cycle(Entity<BallisticAmmoProviderComponent> ent, MapCoordinates coordinates);

    private void OnBallisticInit(Entity<BallisticAmmoProviderComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = Containers.EnsureContainer<Container>(ent, "ballistic-ammo");
        // TODO: This is called twice though we need to support loading appearance data (and we need to call it on MapInit
        // to ensure it's correct).
        UpdateBallisticAppearance(ent);
    }

    private void OnBallisticMapInit(Entity<BallisticAmmoProviderComponent> ent, ref MapInitEvent args)
    {
        // TODO this should be part of the prototype, not set on map init.
        // Alternatively, just track spawned count, instead of unspawned count.
        if (ent.Comp.Proto != null)
        {
            ent.Comp.UnspawnedCount = Math.Max(0, ent.Comp.Capacity - ent.Comp.Container.ContainedEntities.Count);
            UpdateBallisticAppearance(ent);
            DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.UnspawnedCount));
        }
    }

    protected int GetBallisticShots(BallisticAmmoProviderComponent component)
    {
        return component.Entities.Count + component.UnspawnedCount;
    }

    private void OnBallisticTakeAmmo(Entity<BallisticAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            EntityUid entity;

            if (ent.Comp.Entities.Count > 0)
            {
                entity = ent.Comp.Entities[^1];

                args.Ammo.Add((entity, EnsureShootable(entity)));
                ent.Comp.Entities.RemoveAt(ent.Comp.Entities.Count - 1);
                DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.Entities));
                Containers.Remove(entity, ent.Comp.Container);
            }
            else if (ent.Comp.UnspawnedCount > 0)
            {
                ent.Comp.UnspawnedCount--;
                DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.UnspawnedCount));
                ammoEntity = Spawn(ent.Comp.Proto, args.Coordinates);
            }

            if (ammoEntity is not { } ammoEnt)
                continue;

            args.Ammo.Add((ammoEnt, EnsureShootable(ammoEnt)));
            if (TryComp<BallisticAmmoSelfRefillerComponent>(ent, out var refiller))
            {
                PauseSelfRefill((ent, refiller));
            }
        }

        UpdateBallisticAppearance(ent);
    }

    private void OnBallisticAmmoCount(Entity<BallisticAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count = GetBallisticShots(ent.Comp);
        args.Capacity = ent.Comp.Capacity;
    }

    /// <summary>
    /// Causes <paramref name="entity"/> to pause its refilling for either at least <paramref name="overridePauseDuration"/>
    /// (if not null) or the entity's <see cref="BallisticAmmoSelfRefillerComponent.AutoRefillPauseDuration"/>. If the
    /// entity's next refill would occur after the pause duration, this function has no effect.
    /// </summary>
    public void PauseSelfRefill(
        Entity<BallisticAmmoSelfRefillerComponent> entity,
        TimeSpan? overridePauseDuration = null
    )
    {
        if (overridePauseDuration == null && !entity.Comp.FiringPausesAutoRefill)
            return;

        var nextRefillByPause = Timing.CurTime + (overridePauseDuration ?? entity.Comp.AutoRefillPauseDuration);
        if (nextRefillByPause > entity.Comp.NextAutoRefill)
        {
            entity.Comp.NextAutoRefill = nextRefillByPause;
            DirtyField(entity.AsNullable(), nameof(BallisticAmmoSelfRefillerComponent.NextAutoRefill));
        }
    }

    /// <summary>
    /// Returns true if the given <paramref name="entity"/>'s ballistic ammunition is full, false otherwise.
    /// </summary>
    public bool IsFull(Entity<BallisticAmmoProviderComponent> entity)
    {
        return GetBallisticShots(entity.Comp) >= entity.Comp.Capacity;
    }

    /// <summary>
    /// Returns whether or not <paramref name="inserted"/> can be inserted into <paramref name="entity"/>, based on
    /// available space and whitelists.
    /// </summary>
    public bool CanInsertBallistic(Entity<BallisticAmmoProviderComponent> entity, EntityUid inserted)
    {
        return !_whitelistSystem.IsWhitelistFailOrNull(entity.Comp.Whitelist, inserted) &&
               !IsFull(entity);
    }

    /// <summary>
    /// Attempts to insert <paramref name="inserted"/> into <paramref name="entity"/> as ammunition. Returns true on
    /// success, false otherwise.
    /// </summary>
    public bool TryBallisticInsert(
        Entity<BallisticAmmoProviderComponent> entity,
        EntityUid inserted,
        EntityUid? user,
        bool suppressInsertionSound = false
    )
    {
        if (!CanInsertBallistic(entity, inserted))
            return false;

        entity.Comp.Entities.Add(inserted);
        Containers.Insert(inserted, entity.Comp.Container);
        if (!suppressInsertionSound)
        {
            Audio.PlayPredicted(entity.Comp.SoundInsert, entity, user);
        }

        UpdateBallisticAppearance(entity);
        UpdateAmmoCount(entity);
        DirtyField(entity.AsNullable(), nameof(BallisticAmmoProviderComponent.Entities));

        return true;
    }

    public void UpdateBallisticAppearance(Entity<BallisticAmmoProviderComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        Appearance.SetData(ent, AmmoVisuals.AmmoCount, GetBallisticShots(ent.Comp), appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoMax, ent.Comp.Capacity, appearance);
    }

    public void SetBallisticUnspawned(Entity<BallisticAmmoProviderComponent> entity, int count)
    {
        if (entity.Comp.UnspawnedCount == count)
            return;

        entity.Comp.UnspawnedCount = count;
        UpdateBallisticAppearance(entity);
        UpdateAmmoCount(entity.Owner);
        Dirty(entity);
    }
}

/// <summary>
/// DoAfter event for filling one ballistic ammo provider from another.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AmmoFillDoAfterEvent : SimpleDoAfterEvent
{
}

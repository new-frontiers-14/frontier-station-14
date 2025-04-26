using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Shared._NF.Interaction.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.DoAfter;
using Content.Shared._NF.Interaction.Events;

namespace Content.Shared._NF.Interaction.Systems;

/// <summary>
/// A system for RP fluff items - display a popup after some amount of time and optionally trigger other things.
/// </summary>
public sealed class InteractionPopupOnUseSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractionPopupOnUseComponent, UseInHandEvent>(OnUseOnSelf);
        SubscribeLocalEvent<InteractionPopupOnUseComponent, AfterInteractEvent>(OnUseOnOthers);
        SubscribeLocalEvent<InteractionPopupOnUseComponent, GetVerbsEvent<UtilityVerb>>(AddVerb);
        SubscribeLocalEvent<InteractionPopupOnUseComponent, InteractionPopupOnUseDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Perform an interaction on yourself.
    /// </summary>
    private void OnUseOnSelf(Entity<InteractionPopupOnUseComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = Interact(args.User, args.User, entity, entity.Comp);
    }

    /// <summary>
    /// Perform an interaction on somebody else.
    /// </summary>
    private void OnUseOnOthers(Entity<InteractionPopupOnUseComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = Interact(args.User, args.Target.Value, entity, entity.Comp);
    }

    /// <summary>
    /// Interaction logic - checks target validity, prints out messages ad hoc, starts a doafter for delayed interactions.
    /// </summary>
    public bool Interact(EntityUid user, EntityUid target, EntityUid item, InteractionPopupOnUseComponent comp)
    {
        bool self = target == user;
        InteractionData data;

        // Get our strings to print out.  If we don't have any strings to print, great.
        if (self)
        {
            if (comp.Self == null)
                return false;
            data = comp.Self.Value;
        }
        else
        {
            if (comp.Others == null)
                return false;
            data = comp.Others.Value;
        }

        if (_whitelist.IsWhitelistFail(comp.Whitelist, target))
        {
            if (data.WhitelistFailed != null && _net.IsClient && _gameTiming.IsFirstTimePredicted)
            {
                var msg = Loc.GetString(data.WhitelistFailed, ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupEntity(msg, user, Filter.Local(), true);
            }
            return false;
        }

        if (data.Delay.TotalSeconds <= 0)
        {
            DoPopup(user, target, item, comp);
        }
        else
        {
            if (data.Observers.Start != null)
                ShowPopupForObservers(user, target, data.Observers.Start);

            if (_net.IsClient && !self && data.Actor.Start != null) // Filter by client before we process this string.
            {
                var msg = Loc.GetString(data.Actor.Start, ("target", Identity.Entity(target, EntityManager)));
                _popup.PopupClient(msg, target, user);
            }

            if (_net.IsServer && data.Target.Start != null)
                ShowPopupForTarget(user, target, data.Target.Start);

            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, data.Delay, new InteractionPopupOnUseDoAfterEvent(), item, target: target, used: item)
            {
                NeedHand = true,
                BreakOnMove = true,
            });
        }

        return true;
    }

    private void OnDoAfter(Entity<InteractionPopupOnUseComponent> entity, ref InteractionPopupOnUseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted || args.Target == null)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value))
            return;

        DoPopup(args.User, args.Target.Value, entity, entity.Comp);

        args.Handled = true;
    }

    /// <summary>
    /// Spawns a popup, plays associated sounds, runs a trigger, and optionally spawns entities depending on success/failure.
    /// </summary>
    /// <remarks>
    /// Based largely on InteractionPopupSystem.SharedInteract.
    /// </remarks>
    private void DoPopup(EntityUid user, EntityUid target, EntityUid item, InteractionPopupOnUseComponent comp)
    {
        var predict = (comp.SuccessChance <= 0f || comp.SuccessChance >= 1f)
                      && comp.InteractSuccessSpawn == null
                      && comp.InteractFailureSpawn == null;

        if (_net.IsClient && !predict)
            return;

        var self = user == target;
        InteractionData data;
        if (self)
        {
            if (comp.Self == null)
                return;
            data = comp.Self.Value;
        }
        else
        {
            if (comp.Others == null)
                return;
            data = comp.Others.Value;
        }

        string? actorMsg = null; // Stores the text to be shown to the actor in the popup message.
        SoundSpecifier? sfx = null; // Stores the filepath of the sound to be played

        if (_random.Prob(comp.SuccessChance))
        {
            if (data.Observers.Success != null)
                ShowPopupForObservers(user, target, data.Observers.Success);

            if (data.Actor.Success != null)
                actorMsg = Loc.GetString(data.Actor.Success, ("target", Identity.Entity(target, EntityManager)));

            if (_net.IsServer && !self && data.Target.Success != null)
                ShowPopupForTarget(user, target, data.Target.Success);

            if (comp.InteractSuccessSound != null)
                sfx = comp.InteractSuccessSound;

            if (comp.InteractSuccessSpawn != null)
                Spawn(comp.InteractSuccessSpawn, _transform.GetMapCoordinates(target));

            var ev = new InteractionPopupOnUseSuccessEvent(item, user, target);
            RaiseLocalEvent(item, ref ev);
        }
        else
        {
            if (data.Observers.Failure != null)
                ShowPopupForObservers(user, target, data.Observers.Failure);

            if (data.Actor.Failure != null)
                actorMsg = Loc.GetString(data.Actor.Failure, ("target", Identity.Entity(target, EntityManager)));

            if (_net.IsServer && !self && data.Target.Failure != null)
                ShowPopupForTarget(user, target, data.Target.Failure);

            if (comp.InteractFailureSound != null)
                sfx = comp.InteractFailureSound;

            if (comp.InteractFailureSpawn != null)
                Spawn(comp.InteractFailureSpawn, _transform.GetMapCoordinates(target));

            var ev = new InteractionPopupOnUseFailureEvent(item, user, target);
            RaiseLocalEvent(item, ref ev);
        }

        if (!predict)
        {
            if (actorMsg != null)
                _popup.PopupEntity(actorMsg, target, user);

            if (comp.SoundPerceivedByOthers)
                _audio.PlayPvs(sfx, target);
            else
                _audio.PlayEntity(sfx, Filter.Entities(user, target), target, false);
            return;
        }

        if (actorMsg != null)
            _popup.PopupClient(actorMsg, target, user);

        if (sfx == null)
            return;

        if (comp.SoundPerceivedByOthers || _net.IsClient)
            _audio.PlayPredicted(sfx, target, user);
        else
            _audio.PlayEntity(sfx, Filter.Empty().FromEntities(target), target, false);
    }

    private void ShowPopupForObservers(EntityUid user, EntityUid target, string msgLoc)
    {
        var msgOthers = Loc.GetString(msgLoc,
            ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(msgOthers, user, Filter.PvsExcept(user, entityManager: EntityManager).RemovePlayerByAttachedEntity(target), true);
    }

    private void ShowPopupForTarget(EntityUid user, EntityUid target, string msgLoc)
    {
        var msgTarget = Loc.GetString(msgLoc,
            ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(msgTarget, user, target);
    }

    private void AddVerb(Entity<InteractionPopupOnUseComponent> entity, ref GetVerbsEvent<UtilityVerb> ev)
    {
        if (entity.Owner == ev.User ||
            ev.Using == null ||
            entity.Comp.VerbUse == null ||
            !ev.CanInteract ||
            !ev.CanAccess ||
            _whitelist.IsWhitelistFail(entity.Comp.Whitelist, ev.Target))
            return;

        var user = ev.User;
        UtilityVerb verb = new()
        {
            Act = () =>
            {
                Interact(user, user, entity, entity.Comp);
            },
            Text = Loc.GetString(entity.Comp.VerbUse.Value),
            Priority = -1
        };

        ev.Verbs.Add(verb);
    }
}

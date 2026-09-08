using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Wieldable;

namespace Content.Shared._NF.Traits;

public sealed class OneHandParalyzedSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedVerbSystem _verbSystem = default!;

   ISawmill _oneHandParalyzedLogger = Logger.GetSawmill("one-hand-paralyzed");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OneHandParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<OneHandParalyzedComponent, PickupAttemptEvent>(OnPickUpAttempt);
        SubscribeLocalEvent<OneHandParalyzedComponent, WieldAttemptEvent>(OnWieldAttempt);
        SubscribeLocalEvent<OneHandParalyzedComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<OneHandParalyzedComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<OneHandParalyzedComponent, InteractionAttemptEvent>(OnInteractionAttemptEvent);
    }



    private void OnStartup(Entity<OneHandParalyzedComponent> ent, ref ComponentStartup args)
    {
        // Sets the paralyzed hand to the active one (currently just the right hand) only once.
        ent.Comp.ParalyzedHand ??= _sharedHandsSystem.GetActiveHand(ent.Owner);
        Dirty(ent);
    }

    private bool UsingParalyzedHand(Entity<OneHandParalyzedComponent> ent)
    {
        return _sharedHandsSystem.GetActiveHand(ent.Owner) == ent.Comp.ParalyzedHand;
    }

    private void OnPickUpAttempt(Entity<OneHandParalyzedComponent> ent, ref PickupAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Can't carry item that requires two hands if you have 2 (or less) hands and one of them is paralyzed.
        var itemTooBig = HasComp<MultiHandedItemComponent>(args.Item) && _sharedHandsSystem.GetHandCount(args.User) <= 2;

        if (!UsingParalyzedHand(ent) && !itemTooBig)
            return;

        var message = Loc.GetString("trait-one-hand-paralyzed-pickup-attempt", ("item", Identity.Entity(args.Item, EntityManager)));
        _popupSystem.PopupClient(message, ent, ent, PopupType.SmallCaution);
        args.Cancel();
    }

    private void OnInteractionAttemptEvent(Entity<OneHandParalyzedComponent> ent, ref InteractionAttemptEvent args)
    {
        if (UsingParalyzedHand(ent))
        {
            // If target is null or an entity with ActivatableUI, do nothing?
            if (args.Target is not { } target || HasComp<ActivatableUIComponent>(target))
            {
            }
            else if (HasComp<ItemToggleComponent>(target) || HasComp<TriggerOnActivateComponent>(target))
            {
                args.Cancelled = true;
                var message = Loc.GetString("trait-one-hand-paralyzed-activate-attempt",
                    ("item", Identity.Entity(target, EntityManager)));
                _popupSystem.PopupClient(message, ent, ent, PopupType.SmallCaution);
            }
        }
    }

    private void OnPullAttempt(Entity<OneHandParalyzedComponent> ent, ref PullAttemptEvent args)
        {
            TryComp(ent.Owner, out PullerComponent? pullerComp);
            if (args.Cancelled || !pullerComp!.NeedsHands)
                return;

            // Can't pull item that requires two hands if you have 2 (or less?) hands and one of them is paralyzed.
            var itemTooBig = HasComp<MultiHandedItemComponent>(args.PulledUid) && _sharedHandsSystem.GetHandCount(args.PullerUid) <= 2;

            if (!UsingParalyzedHand(ent) && !itemTooBig)
                return;

            var message = Loc.GetString("trait-one-hand-paralyzed-pull-attempt", ("item", Identity.Entity(args.PulledUid, EntityManager)));
            _popupSystem.PopupClient(message, ent.Owner, ent.Owner, PopupType.SmallCaution);
            args.Cancelled = true;
        }

        private void OnWieldAttempt(Entity<OneHandParalyzedComponent> ent, ref WieldAttemptEvent args)
        {
            if (args.Cancelled)
                return;
            args.Cancelled = true;

            var selfMessage = Loc.GetString("trait-one-hand-paralyzed-wield-message", ("item", args.Wielded));
            var othersMessage = Loc.GetString("trait-one-hand-paralyzed-wield-message-other", ("user", Identity.Entity(args.User, EntityManager)), ("item", args.Wielded));
            _popupSystem.PopupPredicted(selfMessage, othersMessage, args.User, args.User, PopupType.SmallCaution);
        }

        private void OnActivateInWorld(Entity<OneHandParalyzedComponent> ent, ref ActivateInWorldEvent args)
        {
            if (args.Handled)
            {
                return;
            }

            if (!UsingParalyzedHand(ent) || (ent.Owner == args.Target &&
                                             TryComp<BuckleComponent>(ent.Owner, out var buckleComponent) &&
                                             !buckleComponent.Buckled))
                return;

            var message = "";

            // Fallback for if the target is an item and/or isn't something to buckle into.
            if (HasComp<ItemToggleComponent>(args.Target) || !HasComp<StrapComponent>(args.Target))
            {
                message = Loc.GetString("trait-one-hand-paralyzed-fallback");
            }

            // If we got to this point and there's no message, run a regular InteractHandEvent on the target.
            if (message == "")
            {
                var ev = new InteractHandEvent(ent.Owner, args.Target);
                RaiseLocalEvent(args.Target, ev);
                return;
            }

            args.Handled = true;
            _popupSystem.PopupClient(Loc.GetString(message), ent, ent, PopupType.SmallCaution);
        }
    }

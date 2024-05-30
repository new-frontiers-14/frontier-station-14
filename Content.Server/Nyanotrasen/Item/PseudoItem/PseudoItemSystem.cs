using Content.Server.Actions;
using Content.Server.Bed.Sleep;
using Content.Server.Carrying;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Item.PseudoItem;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.Item.PseudoItem;

public sealed class PseudoItemSystem : EntitySystem
{
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly ItemSystem _itemSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly CarryingSystem _carrying = default!; // Frontier
    [Dependency] private readonly ActionsSystem _actions = default!; // Frontier
    [Dependency] private readonly PopupSystem _popup = default!; // Frontier

    [ValidatePrototypeId<TagPrototype>]
    private const string PreventTag = "PreventLabel";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PseudoItemComponent, GetVerbsEvent<InnateVerb>>(AddInsertVerb);
        SubscribeLocalEvent<PseudoItemComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertAltVerb);
        SubscribeLocalEvent<PseudoItemComponent, EntGotRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<PseudoItemComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
        SubscribeLocalEvent<PseudoItemComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<PseudoItemComponent, PseudoItemInsertDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PseudoItemComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<PseudoItemComponent, TryingToSleepEvent>(OnTrySleeping); // Frontier
    }

    private void AddInsertVerb(EntityUid uid, PseudoItemComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (component.Active)
            return;

        if (!TryComp<StorageComponent>(args.Target, out var targetStorage))
            return;

        if (Transform(args.Target).ParentUid == uid)
            return;

        InnateVerb verb = new()
        {
            Act = () =>
            {
                TryInsert(args.Target, uid, args.User, component, targetStorage);
            },
            Text = Loc.GetString("action-name-insert-self"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void AddInsertAltVerb(EntityUid uid, PseudoItemComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (args.User == args.Target)
            return;

        if (component.Active)
            return;

        if (args.Hands == null)
            return;

        if (!TryComp<StorageComponent>(args.Hands.ActiveHandEntity, out var targetStorage))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartInsertDoAfter(args.User, uid, args.Hands.ActiveHandEntity.Value, component);
            },
            Text = Loc.GetString("action-name-insert-other", ("target", Identity.Entity(args.Target, EntityManager))),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void OnEntRemoved(EntityUid uid, PseudoItemComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (!component.Active)
            return;

        RemComp<ItemComponent>(uid);
        component.Active = false;

        // Frontier
        if (component.SleepAction is { Valid: true })
            _actions.RemoveAction(uid, component.SleepAction);
    }

    private void OnGettingPickedUpAttempt(EntityUid uid, PseudoItemComponent component,
        GettingPickedUpAttemptEvent args)
    {
        if (args.User == args.Item)
            return;

        // Frontier: prevent people from pushing each other from a bag
        if (HasComp<ItemComponent>(args.User))
        {
            args.Cancel();
            return;
        }

        // Frontier: try to carry the person when taking them out of a bag.
        if (_carrying.TryCarry(args.User, uid))
        {
            args.Cancel();
            return;
        }

        Transform(uid).AttachToGridOrMap();
        args.Cancel();
    }

    private void OnDropAttempt(EntityUid uid, PseudoItemComponent component, DropAttemptEvent args)
    {
        if (component.Active)
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, PseudoItemComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used == null)
            return;

        args.Handled = TryInsert(args.Args.Used.Value, uid, args.User, component);
    }

    public bool TryInsert(EntityUid storageUid, EntityUid toInsert, EntityUid userUid, PseudoItemComponent component,
        StorageComponent? storage = null)
    {
        if (!Resolve(storageUid, ref storage))
            return false;

        var item = EnsureComp<ItemComponent>(toInsert);
        _tagSystem.TryAddTag(toInsert, PreventTag);
        _itemSystem.SetSize(toInsert, component.Size, item);
        _itemSystem.VisualsChanged(toInsert);

        if (!_storageSystem.CanInsert(storageUid, toInsert, out _) ||
            !_storageSystem.PlayerInsertEntityInWorld(storageUid, userUid, toInsert))
        {
            component.Active = false;
            RemComp<ItemComponent>(toInsert);
            return false;
        }
        _storageSystem.UpdateUI(storageUid);
        _storageSystem.UpdateAppearance(storageUid);

        // Frontier
        if (HasComp<AllowsSleepInsideComponent>(storageUid))
            _actions.AddAction(toInsert, ref component.SleepAction, SleepingSystem.SleepActionId, toInsert);

        component.Active = true;
        return true;
    }

    private void StartInsertDoAfter(EntityUid inserter, EntityUid toInsert, EntityUid storageEntity,
        PseudoItemComponent? pseudoItem = null)
    {
        if (!Resolve(toInsert, ref pseudoItem))
            return;

        var ev = new PseudoItemInsertDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, inserter, 5f, ev, toInsert, toInsert, storageEntity)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnInsertAttempt(EntityUid uid, PseudoItemComponent component,
        ContainerGettingInsertedAttemptEvent args)
    {
        if (!component.Active)
            return;
        // This hopefully shouldn't trigger, but this is a failsafe just in case so we dont bluespace them cats
        args.Cancel();
    }

    // Frontier - show a popup when a pseudo-item falls asleep inside a bag.
    private void OnTrySleeping(EntityUid uid, PseudoItemComponent component, TryingToSleepEvent args)
    {
        var parent = Transform(uid).ParentUid;
        if (!HasComp<SleepingComponent>(uid) && parent is { Valid: true } && HasComp<AllowsSleepInsideComponent>(parent))
            _popup.PopupEntity(Loc.GetString("popup-sleep-in-bag", ("entity", uid)), uid);
    }
}

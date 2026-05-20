using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Whitelist;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Shared._DV.Silicons.Borgs;

/// <summary>
/// Handles all id chip interaction with borgs.
/// </summary>
public sealed class IdChipSlotSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedWiresSystem _wires = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdChipSlotComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<IdChipSlotComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<IdChipSlotComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<IdChipSlotComponent, EntInsertedIntoContainerMessage>(OnChipInserted);
        SubscribeLocalEvent<IdChipSlotComponent, EntRemovedFromContainerMessage>(OnChipRemoved);
        Subs.BuiEvents<IdChipSlotComponent>(BorgUiKey.Key, subs =>
        {
            subs.Event<BorgEjectIdChipMessage>(OnEjectMessage);
        });
    }

    private void OnStartup(Entity<IdChipSlotComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    private void OnInteractUsing(Entity<IdChipSlotComponent> ent, ref AfterInteractUsingEvent args)
    {
        var user = args.User;
        if (args.Handled || !args.CanReach || ent.Owner == user || ent.Comp.Chip != null)
            return;

        // ignore non-chip items
        var chip = args.Used;
        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, chip))
            return;

        args.Handled = true;

        if (!_wires.IsPanelOpen(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("borg-panel-not-open"), ent, user);
            return;
        }

        _container.Insert(chip, ent.Comp.Container);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} installed id chip {ToPrettyString(chip)} into borg {ToPrettyString(ent)}");
    }

    private void OnGetAdditionalAccess(Entity<IdChipSlotComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.Chip is {} chip)
            args.Entities.Add(chip);
    }

    private void OnChipInserted(Entity<IdChipSlotComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        // enable its access so the borg can use it
        _access.SetAccessEnabled(args.Entity, true);
    }

    private void OnChipRemoved(Entity<IdChipSlotComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        // disable its access so it's unusable outside of a borg
        _access.SetAccessEnabled(args.Entity, false);
    }

    private void OnEjectMessage(Entity<IdChipSlotComponent> ent, ref BorgEjectIdChipMessage args)
    {
        if (ent.Comp.Chip is not {} chip)
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} removed id chip {ToPrettyString(chip)} from borg {ToPrettyString(ent)}");
        _container.Remove(chip, ent.Comp.Container);
        _hands.TryPickupAnyHand(args.Actor, chip);
    }
}

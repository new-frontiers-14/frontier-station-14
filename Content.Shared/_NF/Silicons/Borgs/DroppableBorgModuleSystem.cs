using Content.Shared._NF.Interaction.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Silicons.Borgs;

public sealed class DroppableBorgModuleSystem : EntitySystem
{
    [Dependency] private readonly HandPlaceholderSystem _placeholder = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DroppableBorgModuleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DroppableBorgModuleComponent, BorgCanInsertModuleEvent>(OnCanInsertModule);
        SubscribeLocalEvent<DroppableBorgModuleComponent, BorgModuleSelectedEvent>(OnModuleSelected);
        SubscribeLocalEvent<DroppableBorgModuleComponent, BorgModuleUnselectedEvent>(OnModuleUnselected);
    }

    private void OnMapInit(Entity<DroppableBorgModuleComponent> ent, ref MapInitEvent args)
    {
        var items = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        var placeholders = _container.EnsureContainer<Container>(ent, ent.Comp.PlaceholderContainerId);

        foreach (var slot in ent.Comp.Items)
        {
            // only the server runs mapinit, this wont make clientside entities
            var successful = TrySpawnInContainer(slot.Id, ent, ent.Comp.ContainerId, out var item);
            // this would only fail if the current entity is being terminated, which is impossible for mapinit
            DebugTools.Assert(successful, $"Somehow failed to insert {ToPrettyString(item)} into {ToPrettyString(ent)}");
            var placeholderUid = _placeholder.SpawnPlaceholder(placeholders, item!.Value, slot.Id, slot.Whitelist, slot.Blacklist);
            if (slot.DisplayName != null)
                _meta.SetEntityName(placeholderUid, Loc.GetString(slot.DisplayName));
        }

        foreach (var placeholder in ent.Comp.Placeholders)
        {
            var placeholderUid = _placeholder.SpawnPlaceholder(placeholders, EntityUid.Invalid, placeholder.Id, placeholder.Whitelist, placeholder.Blacklist);
            if (placeholder.DisplayName != null)
                _meta.SetEntityName(placeholderUid, Loc.GetString(placeholder.DisplayName));
            _container.Insert(placeholderUid, items, force: true);
        }

        Dirty(ent);
    }

    private void OnCanInsertModule(Entity<DroppableBorgModuleComponent> ent, ref BorgCanInsertModuleEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var module in args.Chassis.Comp.ModuleContainer.ContainedEntities)
        {
            if (!TryComp<DroppableBorgModuleComponent>(module, out var comp))
                continue;

            if (comp.ModuleId != ent.Comp.ModuleId)
                continue;

            if (args.User is { } user)
                _popup.PopupEntity(Loc.GetString("borg-module-duplicate"), args.Chassis, user); // event is only raised by server so not using PopupClient
            args.Cancelled = true;
            return;
        }
    }

    private void OnModuleSelected(Entity<DroppableBorgModuleComponent> ent, ref BorgModuleSelectedEvent args)
    {
        var chassis = args.Chassis;
        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        var container = _container.GetContainer(ent, ent.Comp.ContainerId);
        var items = container.ContainedEntities;
        for (int i = 0; i < ent.Comp.Items.Count; i++)
        {
            AddItemAsHand((chassis, hands), items[0], HandId(ent, i)); // the contained items will gradually go to 0
        }
        for (int i = 0; i < ent.Comp.Placeholders.Count; i++)
        {
            AddItemAsHand((chassis, hands), items[0], PlaceholderHandId(ent, i)); // the contained items will gradually go to 0
        }
    }

    private void OnModuleUnselected(Entity<DroppableBorgModuleComponent> ent, ref BorgModuleUnselectedEvent args)
    {
        var chassis = args.Chassis;
        if (!TryComp<HandsComponent>(chassis, out var hands))
            return;

        if (TerminatingOrDeleted(ent))
        {
            for (int i = 0; i < ent.Comp.Items.Count; i++)
            {
                if (!DeleteHandAndHeldItem((chassis, hands), HandId(ent, i)))
                    Log.Error($"Borg {ToPrettyString(chassis)} terminated with empty hand {i} in {ToPrettyString(ent)}");
            }
            for (int i = 0; i < ent.Comp.Placeholders.Count; i++)
            {
                if (!DeleteHandAndHeldItem((chassis, hands), PlaceholderHandId(ent, i)))
                    Log.Error($"Borg {ToPrettyString(chassis)} terminated with empty hand {i} in {ToPrettyString(ent)}");
            }
            return;
        }

        var container = _container.GetContainer(ent, ent.Comp.ContainerId);
        for (int i = 0; i < ent.Comp.Items.Count; i++)
        {
            if (!DeleteHandAndStoreItem((chassis, hands), container, HandId(ent, i)))
                Log.Error($"Borg {ToPrettyString(chassis)} had an empty hand in the slot for {ent.Comp.Items[i].Id}");
        }
        for (int i = 0; i < ent.Comp.Placeholders.Count; i++)
        {
            if (!DeleteHandAndStoreItem((chassis, hands), container, PlaceholderHandId(ent, i)))
                Log.Error($"Borg {ToPrettyString(chassis)} had an empty hand in the slot for {ent.Comp.Items[i].Id}");
        }
    }

    /// <summary>
    /// Format the hand ID for a given module and item number.
    /// </summary>
    private static string HandId(EntityUid uid, int i)
    {
        return $"nf-{uid}-item-{i}";
    }

    /// <summary>
    /// Format the hand ID for a given module and placeholder item number.
    /// </summary>
    private static string PlaceholderHandId(EntityUid uid, int i)
    {
        return $"nf-{uid}-placeholder-{i}";
    }

    /// <summary>
    /// Tries to add a hand to the given cyborg entity and insert the given item into it.
    /// On failure, the hand will be deleted.
    /// </summary>
    private void AddItemAsHand(Entity<HandsComponent> chassis, EntityUid item, string handId)
    {
        _hands.AddHand(chassis, handId, HandLocation.Middle, chassis.Comp);
        var hand = chassis.Comp.Hands[handId];
        _hands.DoPickup(chassis, hand, item, chassis.Comp);
        if (hand.HeldEntity != item)
        {
            Log.Error($"Failed to pick up {ToPrettyString(item)} into hand {handId} of {ToPrettyString(chassis)}, it holds {ToPrettyString(hand.HeldEntity)}");
            // If we didn't pick up our expected item, delete the hand.  No free hands!
            _hands.RemoveHand(chassis, handId, chassis.Comp);
        }

        _interaction.DoContactInteraction(chassis, item); // for potential forensics or other systems (why does hands system not do this)
        _placeholder.SetEnabled(item, true);
    }

    /// <summary>
    /// Removes the named hand from the given cyborg, returning its item to the given container.
    /// </summary>
    private bool DeleteHandAndStoreItem(Entity<HandsComponent> chassis, BaseContainer container, string handId)
    {
        var ret = true;
        _hands.TryGetHand(chassis, handId, out var hand, chassis.Comp);
        if (hand?.HeldEntity is { } item)
        {
            _placeholder.SetEnabled(item, false);
            _container.Insert(item, container, force: true);
        }
        else
            ret = false;

        _hands.RemoveHand(chassis, handId, chassis.Comp);
        return ret;
    }

    /// <summary>
    /// Removes the named hand from the given cyborg, deleting its held item.
    /// </summary>
    private bool DeleteHandAndHeldItem(Entity<HandsComponent> chassis, string handId)
    {
        bool ret = true;
        _hands.TryGetHand(chassis, handId, out var hand, chassis.Comp);

        if (hand?.HeldEntity is { } item)
            QueueDel(item);
        else if (!TerminatingOrDeleted(chassis) && Transform(chassis).MapID != MapId.Nullspace) // don't care if its empty if the server is shutting down
            ret = false;

        _hands.RemoveHand(chassis, handId, chassis.Comp);
        return ret;
    }
}

/// <summary>
/// Event raised on a module to check if it can be installed.
/// This should exist upstream but doesn't.
/// </summary>
[ByRefEvent]
public record struct BorgCanInsertModuleEvent(Entity<BorgChassisComponent> Chassis, EntityUid? User, bool Cancelled = false);

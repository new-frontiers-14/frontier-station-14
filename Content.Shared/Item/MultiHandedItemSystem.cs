using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.Item;

public sealed class MultiHandedItemSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MultiHandedItemComponent, GettingPickedUpAttemptEvent>(OnAttemptPickup);
        SubscribeLocalEvent<MultiHandedItemComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<MultiHandedItemComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<MultiHandedItemComponent, GotUnequippedHandEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<MultiHandedItemComponent> ent, ref GotEquippedHandEvent args)
    {
        // Frontier
        if (_timing.ApplyingState)
        {
            // If we're currently applying server-side state, we'll get the virtual item anyway and don't need to
            // create it client-side; this avoids a debug assertion when a client joins in exactly the same tick a
            // multi-handed item is picked up
            return;
        }
        // End Frontier

        for (var i = 0; i < ent.Comp.HandsNeeded - 1; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.User);
        }
    }

    private void OnUnequipped(Entity<MultiHandedItemComponent> ent, ref GotUnequippedHandEvent args)
    {
        _virtualItem.DeleteInHandsMatching(args.User, ent.Owner);
    }

    private void OnAttemptPickup(Entity<MultiHandedItemComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (_hands.CountFreeHands(args.User) >= ent.Comp.HandsNeeded)
            return;

        args.Cancel();
        _popup.PopupPredictedCursor(Loc.GetString("multi-handed-item-pick-up-fail",
            ("number", ent.Comp.HandsNeeded - 1), ("item", ent.Owner)), args.User);
    }

    private void OnVirtualItemDeleted(Entity<MultiHandedItemComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != ent.Owner || _timing.ApplyingState)
            return;

        _hands.TryDrop(args.User, ent.Owner);
    }
}

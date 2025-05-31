using Content.Shared.Movement.Systems;
using Content.Shared._NF.Movement.Components;
using Content.Shared.Inventory;
using Content.Shared.Clothing;

namespace Content.Shared._NF.Movement;

/// <summary>
/// Changes the friction and acceleration of an entity depending on if they have an inventory slot full.
/// </summary>
public sealed class InventorySlotFrictionSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _move = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventorySlotFrictionComponent, ClothingDidEquippedEvent>(OnDidEquipped);
        SubscribeLocalEvent<InventorySlotFrictionComponent, ClothingDidUnequippedEvent>(OnDidUnequipped);
        SubscribeLocalEvent<InventorySlotFrictionComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
    }

    /// <remarks>
    /// A bit naive, could apply only when the particular slot is filled/emptied.
    /// </remarks>
    private void OnDidEquipped(Entity<InventorySlotFrictionComponent> ent, ref ClothingDidEquippedEvent args)
    {
        _move.RefreshFrictionModifiers(ent);
    }

    public void OnDidUnequipped(Entity<InventorySlotFrictionComponent> ent, ref ClothingDidUnequippedEvent args)
    {
        _move.RefreshFrictionModifiers(ent);
    }

    /// <summary>
    /// Refreshing friction modifiers: check for inventory slot item, adjust friction if needed.
    /// </summary>
    private void OnRefreshFrictionModifiers(Entity<InventorySlotFrictionComponent> ent,
        ref RefreshFrictionModifiersEvent args)
    {
        if (_inventory.TryGetSlotEntity(ent, ent.Comp.Slot, out var _) == ent.Comp.Full)
        {
            args.ModifyFriction(ent.Comp.Friction, ent.Comp.FrictionNoInput);
            args.ModifyAcceleration(ent.Comp.Acceleration);
        }
    }
}

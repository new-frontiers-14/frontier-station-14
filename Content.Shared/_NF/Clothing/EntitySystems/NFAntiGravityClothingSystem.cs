using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Alert;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Robust.Shared.Containers;
using Content.Shared.Atmos.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class NFAntiGravityClothingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NFAntiGravityClothingComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<NFAntiGravityClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<NFAntiGravityClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<NFAntiGravityClothingComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<NFAntiGravityClothingComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
    }

    private void OnToggled(Entity<NFAntiGravityClothingComponent> ent, ref ItemToggledEvent args)
    {
        var (uid, comp) = ent;
        // only stick to the floor if being worn in the correct slot
        if (_container.TryGetContainingContainer((uid, null, null), out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, comp.Slot, out var worn)
            && uid == worn)
        {
            UpdateMoonbootEffects(container.Owner, ent, args.Activated);
        }

        var prefix = args.Activated ? "on" : null;
        _item.SetHeldPrefix(ent, prefix);
        _clothing.SetEquippedPrefix(ent, prefix);
    }

    private void OnGotUnequipped(Entity<NFAntiGravityClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateMoonbootEffects(args.Wearer, ent, false);
    }

    private void OnGotEquipped(Entity<NFAntiGravityClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateMoonbootEffects(args.Wearer, ent, _toggle.IsActivated(ent.Owner));
    }

    public void UpdateMoonbootEffects(EntityUid user, Entity<NFAntiGravityClothingComponent> ent, bool state)
    {
        if (state)
            _alerts.ShowAlert(user, ent.Comp.MoonBootsAlert);
        else
            _alerts.ClearAlert(user, ent.Comp.MoonBootsAlert);
    }

    private void OnIsWeightless(Entity<NFAntiGravityClothingComponent> ent, ref IsWeightlessEvent args)
    {
        if (args.Handled || !_toggle.IsActivated(ent.Owner))
            return;

        args.Handled = true;
        args.IsWeightless = true;
    }

    private void OnIsWeightless(Entity<NFAntiGravityClothingComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        OnIsWeightless(ent, ref args.Args);
    }
}

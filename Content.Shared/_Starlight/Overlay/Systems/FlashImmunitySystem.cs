using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Shared.Starlight.Overlay;

public sealed class FlashImmunitySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashImmunityComponent, GotEquippedEvent>(OnFlashImmunityEquipped);
        SubscribeLocalEvent<FlashImmunityComponent, GotUnequippedEvent>(OnFlashImmunityUnEquipped);

        SubscribeLocalEvent<FlashImmunityComponent, ComponentStartup>(OnFlashImmunityChanged);
        SubscribeLocalEvent<FlashImmunityComponent, ComponentRemove>(OnFlashImmunityChanged);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);

        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnVisionChanged);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnVisionChanged);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        var flashImmunityChangedEvent = new FlashImmunityCheckEvent(args.Entity, HasFlashImmunityVisionBlockers(args.Entity));
        RaiseLocalEvent(args.Entity, flashImmunityChangedEvent);
    }

    private void OnFlashImmunityChanged(EntityUid uid, FlashImmunityComponent component, EntityEventArgs args)
    {
        uid = GetPossibleWearer(uid);
        var flashImmunityChangedEvent = new FlashImmunityCheckEvent(uid, HasFlashImmunityVisionBlockers(uid));
        RaiseLocalEvent(uid, flashImmunityChangedEvent);
    }

    private void OnVisionChanged(EntityUid uid, Component component, EntityEventArgs args)
    {
        uid = GetPossibleWearer(uid);
        var flashImmunityChangedEvent = new FlashImmunityCheckEvent(uid, HasFlashImmunityVisionBlockers(uid));
        RaiseLocalEvent(uid, flashImmunityChangedEvent);
    }

    private void OnFlashImmunityEquipped(EntityUid uid, FlashImmunityComponent component, GotEquippedEvent args)
    {
        var flashImmunityChangedEvent = new FlashImmunityCheckEvent(uid, HasFlashImmunityVisionBlockers(args.Equipee));
        RaiseLocalEvent(args.Equipee, flashImmunityChangedEvent);
    }

    private void OnFlashImmunityUnEquipped(EntityUid uid, FlashImmunityComponent component, GotUnequippedEvent args)
    {
        var flashImmunityChangedEvent = new FlashImmunityCheckEvent(uid, HasFlashImmunityVisionBlockers(args.Equipee));
        RaiseLocalEvent(args.Equipee, flashImmunityChangedEvent);
    }

    private EntityUid GetPossibleWearer(EntityUid uid)
    {
        if (!TryComp<ClothingComponent>(uid, out _))
            return uid;

        // We want the wearer of the clothing, not the clothing itself.
        return IoCManager.Resolve<IEntityManager>()
            .GetComponentOrNull<TransformComponent>(uid)?.ParentUid ?? uid;
    }

    public bool HasFlashImmunityVisionBlockers(EntityUid uid)
    {
        if (EntityManager.TryGetComponent(uid, out FlashImmunityComponent? flashImmunityComponent)
            && flashImmunityComponent.Enabled)
            return true;

        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
        {
            var slots = _inventory.GetSlotEnumerator((uid, inventoryComp), SlotFlags.WITHOUT_POCKET);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null &&
                    EntityManager.TryGetComponent(slot.ContainedEntity, out FlashImmunityComponent? wornFlashImmunityComponent) &&
                    wornFlashImmunityComponent.Enabled)
                    return true;
            }
        }

        return false;
    }
}

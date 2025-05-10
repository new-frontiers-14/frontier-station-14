using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing._NF.Components;

namespace Content.Server._NF.Clothing.EntitySystems;

public sealed class AutoToggleableOuterClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ToggleableClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoToggleableOuterClothingComponent, StartingGearEquippedEvent>(OnStartingGear);
    }

    private void OnStartingGear(EntityUid uid, AutoToggleableOuterClothingComponent component, ref StartingGearEquippedEvent args)
    {
        if (TryComp(uid, out InventoryComponent? comp) && _inventory.TryGetSlotEntity(uid, "outerClothing", out var outerClothingEntity, comp) &&
            TryComp<ToggleableClothingComponent>(outerClothingEntity, out var outerClothingSuit))
        {
            _clothing.ToggleClothing(uid, outerClothingEntity.Value, outerClothingSuit);
        }
    }
}

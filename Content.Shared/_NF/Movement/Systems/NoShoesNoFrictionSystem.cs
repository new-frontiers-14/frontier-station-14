using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared._NF.Movement.Components;
using Content.Shared.Whitelist;
using Content.Shared.Inventory;
using Content.Shared.Clothing;

namespace Content.Shared._NF.Movement;

/// <summary>
/// Changes the friction and acceleration of the of a person with no shoes like they are walking on ice.
/// </summary>
public sealed class NoShoesNoFrictionSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoShoesNoFrictionComponent, ClothingDidEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<NoShoesNoFrictionComponent, ClothingDidUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, NoShoesNoFrictionComponent component, ClothingDidEquippedEvent args)
    {
        if (!TryComp(uid, out MovementSpeedModifierComponent? speedModifier))
            return;

        var hasShoes = _inventory.TryGetSlotEntity(uid, component.Slot, out var worn);
        bool blacklist = hasShoes && IsBlacklisted(uid, component, worn);

        if (blacklist)
            UpdateFriction(uid, MovementSpeedModifierComponent.DefaultFriction, MovementSpeedModifierComponent.DefaultFrictionNoInput, MovementSpeedModifierComponent.DefaultAcceleration, speedModifier);
    }

    public void OnGotUnequipped(EntityUid uid, NoShoesNoFrictionComponent component, ClothingDidUnequippedEvent args)
    {
        if (!TryComp(uid, out MovementSpeedModifierComponent? speedModifier))
            return;

        var hasShoes = _inventory.TryGetSlotEntity(uid, component.Slot, out var worn);

        if (!hasShoes)
            UpdateFriction(uid, component.MobFriction, component.MobFrictionNoInput, component.MobAcceleration, speedModifier);
    }

    /// <summary>
    /// Updates the friction and acceleration of an entity based on whether they are wearing shoes.
    /// </summary>
    private void UpdateFriction(EntityUid uid, float friction, float? frictionNoInput, float acceleration, MovementSpeedModifierComponent speedModifier)
    {

        _speedModifierSystem.ChangeFriction(uid, friction, frictionNoInput, acceleration, speedModifier);

    }

    private bool IsBlacklisted(EntityUid uid, NoShoesNoFrictionComponent component, EntityUid? worn)
    {
        return worn.HasValue && _whitelistSystem.IsBlacklistFailOrNull(component.Blacklist, worn.Value);
    }
}

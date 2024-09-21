using Content.Shared.Containers.ItemSlots;
using Content.Shared.Shipyard;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Shipyard.Components;

namespace Content.Shared.Shipyard;

// Note: when adding a new ui key, don't forget to modify the dictionary in SharedShipyardSystem
[NetSerializable, Serializable]
public enum ShipyardConsoleUiKey : byte
{
    Shipyard,
    Security,
    Syndicate,
    BlackMarket,
    Expedition,
    Scrap,
    Sr,
    // Add ships to this key if they are only available from mothership consoles. Shipyards using it are inherently empty and are populated using the ShipyardListingComponent.
    Custom
}

public abstract class SharedShipyardSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, ShipyardConsoleComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ShipyardConsoleComponentState state) return;

    }

    private void OnGetState(EntityUid uid, ShipyardConsoleComponent component, ref ComponentGetState args)
    {

    }

    private void OnComponentInit(EntityUid uid, ShipyardConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, ShipyardConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
    }

    private void OnComponentRemove(EntityUid uid, ShipyardConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.TargetIdSlot);
    }

    [Serializable, NetSerializable]
    private sealed class ShipyardConsoleComponentState : ComponentState
    {
        public List<string> AccessLevels;

        public ShipyardConsoleComponentState(List<string> accessLevels)
        {
            AccessLevels = accessLevels;
        }
    }

}

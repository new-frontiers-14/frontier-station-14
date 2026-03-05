using Content.Server.CartridgeLoader;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Content.Shared.Tag;

namespace Content.Server._NF.CartridgeLoader.Cartridges;

/// <summary>
/// When the user activates a cartridge that has the Plant Manager requisitions store
/// (tag PlantManagerStore + StoreComponent), opens the store UI for that cartridge.
/// Also allows inserting currency into a PDA when it contains such a store cartridge.
/// </summary>
public sealed class PlantManagerRequisitionsCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartridgeLoaderComponent, CartridgeLoaderUiMessage>(OnLoaderUiMessage, after: new[] { typeof(CartridgeLoaderSystem) });
        SubscribeLocalEvent<CurrencyComponent, AfterInteractEvent>(OnCurrencyAfterInteract, after: new[] { typeof(StoreSystem) });
    }

    private void OnLoaderUiMessage(EntityUid loaderUid, CartridgeLoaderComponent component, CartridgeLoaderUiMessage message)
    {
        if (message.Action != CartridgeUiMessageAction.Activate)
            return;

        var cartridge = GetEntity(message.CartridgeUid);
        if (!_tags.HasTag(cartridge, "PlantManagerStore"))
            return;

        if (!HasComp<StoreComponent>(cartridge))
            return;

        _store.ToggleUi(message.Actor, cartridge);
    }

    private void OnCurrencyAfterInteract(EntityUid uid, CurrencyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<CartridgeLoaderComponent>(target, out var loader))
            return;

        var storeEntity = FindStoreInLoader(target, loader);
        if (storeEntity == null)
            return;

        if (!TryComp<StoreComponent>(storeEntity, out var store))
            return;

        if (!_store.TryAddCurrency((uid, component), (storeEntity, store)))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("store-currency-inserted", ("used", uid), ("target", target)), target, args.User);
    }

    private EntityUid? FindStoreInLoader(EntityUid loaderUid, CartridgeLoaderComponent loader)
    {
        foreach (var program in _cartridgeLoader.GetInstalled(loaderUid))
        {
            if (_tags.HasTag(program, "PlantManagerStore") && HasComp<StoreComponent>(program))
                return program;
        }

        if (loader.CartridgeSlot.ContainerSlot?.ContainedEntity is { } slotEntity
            && _tags.HasTag(slotEntity, "PlantManagerStore")
            && HasComp<StoreComponent>(slotEntity))
            return slotEntity;

        return null;
    }
}

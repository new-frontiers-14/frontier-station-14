using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store.Components;

namespace Content.Server.Implants;

public sealed partial class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);
    }

    // TODO: This shouldn't be in the SubdermalImplantSystem
    private void OnStoreRelay(EntityUid uid, StoreComponent store, ImplantRelayEvent<AfterInteractUsingEvent> implantRelay)
    {
        var args = implantRelay.Event;

        if (args.Handled)
            return;

        // can only insert into yourself to prevent uplink checking with renault
        if (args.Target != args.User)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency))
            return;

        // same as store code, but message is only shown to yourself
        if (!_store.TryAddCurrency((args.Used, currency), (uid, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
    }
}

using Content.Server.Station.Systems;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NF_ShipConnectCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NF_ShipConnectCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NF_ShipConnectCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        //SubscribeLocalEvent<LogisticStatsUpdatedEvent>(OnLogisticsStatsUpdated);
        //SubscribeLocalEvent<MailComponent, ComponentInit>(OnComponentInit);
    }

    private void OnUiReady(Entity<NF_ShipConnectCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUI(ent, args.Loader);
    }

    /*private void OnLogisticsStatsUpdated(LogisticStatsUpdatedEvent args)
    {
        UpdateAllCartridges(args.Station);
    }*/

    /*private void OnComponentInit(EntityUid uid, MailComponent mail, ComponentInit args)
    {
        var stationUid = _station.GetOwningStation(uid);
        if (stationUid != null)
            UpdateAllCartridges((EntityUid) stationUid);
    }*/

    private void UpdateAllCartridges(EntityUid station)
    {
        var query = EntityQueryEnumerator<NF_ShipConnectCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            if (cartridge.LoaderUid is not { } loader || comp.Station != station)
                continue;
            UpdateUI((uid, comp), loader);
        }
    }

    private void UpdateUI(Entity<NF_ShipConnectCartridgeComponent> ent, EntityUid loader)
    {
        if (_station.GetOwningStation(loader) is { } station)
            ent.Comp.Station = station;
        //Use this to get info from server
        //if (!TryComp<StationLogisticStatsComponent>(ent.Comp.Station, out var logiStats))
         //   return;

        // Get station's logistic stats
        /*var mailEarnings = logiStats.MailEarnings;
        var damagedMailLosses = logiStats.DamagedMailLosses;
        var expiredMailLosses = logiStats.ExpiredMailLosses;
        var tamperedMailLosses = logiStats.TamperedMailLosses;
        var openedMailCount = logiStats.OpenedMailCount;
        var damagedMailCount = logiStats.DamagedMailCount;
        var expiredMailCount = logiStats.ExpiredMailCount;
        var tamperedMailCount = logiStats.TamperedMailCount;
        var unopenedMailCount = GetUnopenedMailCount();*/

        // Send logistic stats to cartridge client/NF_ShipConnectUiState
        /*var state = new MailMetricUiState(mailEarnings,
                                          damagedMailLosses,
                                          expiredMailLosses,
                                          tamperedMailLosses,
                                          openedMailCount,
                                          damagedMailCount,
                                          expiredMailCount,
                                          tamperedMailCount,
                                          unopenedMailCount);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);*/
    }

    private void OnUiMessage(EntityUid uid, NF_ShipConnectCartridgeComponent component, CartridgeMessageEvent args)
    {
        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, NF_ShipConnectCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

       /* var owningStation = _stationSystem.GetOwningStation(uid);

        if (owningStation is null)
            return;

        var (stationName, entries) = _crewManifest.GetCrewManifest(owningStation.Value);

        var state = new CrewManifestUiState(stationName, entries);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state); ///IMPORTANT UPDATE*/
    }
}

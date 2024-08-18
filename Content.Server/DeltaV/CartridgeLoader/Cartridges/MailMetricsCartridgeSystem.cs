using Content.Server.DeltaV.Cargo.Components;
using Content.Server.DeltaV.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Server.Mail.Components;
using Content.Server._NF.SectorServices;

namespace Content.Server.DeltaV.CartridgeLoader.Cartridges;

public sealed class MailMetricsCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MailMetricsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<LogisticStatsUpdatedEvent>(OnLogisticsStatsUpdated);
        SubscribeLocalEvent<MailComponent, MapInitEvent>(OnMapInit);
    }

    private void OnUiReady(Entity<MailMetricsCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUI(args.Loader);
    }

    private void OnLogisticsStatsUpdated(LogisticStatsUpdatedEvent args)
    {
        UpdateAllCartridges(); // Frontier: remove station
    }

    private void OnMapInit(EntityUid uid, MailComponent mail, MapInitEvent args)
    {
        UpdateAllCartridges(); // Frontier: remove station
    }

    private void UpdateAllCartridges() // Frontier: remove station
    {
        var query = EntityQueryEnumerator<MailMetricsCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            if (cartridge.LoaderUid is not { } loader)
                continue;
            UpdateUI(loader);
        }
    }

    private void UpdateUI(EntityUid loader)
    {
        //if (_station.GetOwningStation(loader) is { } station)
        //    ent.Comp.Station = station;

        if (!TryComp<SectorLogisticStatsComponent>(_sectorService.GetServiceEntity(), out var logiStats))
            return;

        // Get station's logistic stats
        var unopenedMailCount = GetUnopenedMailCount();

        // Send logistic stats to cartridge client
        var state = new MailMetricUiState(logiStats.Metrics, unopenedMailCount);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }


    private int GetUnopenedMailCount() // Frontier: remove EntityUid param
    {
        var unopenedMail = 0;

        var query = EntityQueryEnumerator<MailComponent>();

        while (query.MoveNext(out var _, out var comp))
        {
            //if (comp.IsLocked && _station.GetOwningStation(uid) == station)
            //    unopenedMail++;
            if (comp.IsLocked && comp.IsProfitable) // Frontier: remove station check, add profitable check (consider only possible profit as unopened)
                unopenedMail++;
        }

        return unopenedMail;
    }
}

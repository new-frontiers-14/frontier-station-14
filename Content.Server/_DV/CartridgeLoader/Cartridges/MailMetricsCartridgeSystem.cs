using Content.Server._DV.Cargo.Components;
using Content.Server._DV.Cargo.Systems;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Server._DV.Mail.Components;
using Content.Server._NF.SectorServices; // Frontier

namespace Content.Server._DV.CartridgeLoader.Cartridges;

public sealed class MailMetricsCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MailMetricsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<LogisticStatsUpdatedEvent>(OnLogisticsStatsUpdated);
        SubscribeLocalEvent<MailComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MailMetricsCartridgeComponent, CartridgeMessageEvent>(OnNotificationsUpdated);
    }

    private void OnUiReady(Entity<MailMetricsCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUI(args.Loader, ent.Comp); // Frontier: remove station as first arg
    }

    private void OnNotificationsUpdated(Entity<MailMetricsCartridgeComponent> program, ref CartridgeMessageEvent eventArgs )
    {
        if (eventArgs is MailMetricsNotificationToggleMessage toggleMessage)
            program.Comp.NotificationsEnabled = toggleMessage.NotificationsEnabled;

    }

    private void OnLogisticsStatsUpdated(LogisticStatsUpdatedEvent args)
    {
        UpdateAllCartridges(); // Frontier: remove station
    }

    private void OnMapInit(EntityUid uid, MailComponent mail, MapInitEvent args)
    {
        UpdateAllCartridges(); // Frontier: remove station, no owner check
    }

    private void UpdateAllCartridges() // Frontier: remove station
    {
        var query = EntityQueryEnumerator<MailMetricsCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            if (cartridge.LoaderUid is not { } loader)
                continue;
            UpdateUI(loader, comp);
        }
    }

    private void UpdateUI(EntityUid loader, MailMetricsCartridgeComponent cartComp)
    {
        //if (_station.GetOwningStation(loader) is { } station) // Frontier
        //    ent.Comp.Station = station; // Frontier

        if (!TryComp<SectorLogisticStatsComponent>(_sectorService.GetServiceEntity(), out var logiStats)) // Frontier
            return; // Frontier

        // Get station's logistic stats
        var unopenedMailCount = GetUnopenedMailCount(); // Frontier: no station arg

        // Send logistic stats to cartridge client
        var state = new MailMetricUiState(logiStats.Metrics, unopenedMailCount, cartComp.NotificationsEnabled);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }


    private int GetUnopenedMailCount() // Frontier: remove EntityUid param
    {
        var unopenedMail = 0;

        var query = EntityQueryEnumerator<MailComponent>();

        while (query.MoveNext(out var _, out var comp))
        {
            // Frontier: remove station check, add profitable check (consider only possible profit as unopened)
            if (comp.IsLocked && comp.IsProfitable)
                unopenedMail++;
            // End Frontier
        }

        return unopenedMail;
    }
}

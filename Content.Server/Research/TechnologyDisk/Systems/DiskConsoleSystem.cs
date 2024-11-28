using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.UserInterface;
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Content.Shared.Research.TechnologyDisk.Components;
using Content.Shared.Shipyard.Components;
using Content.Shared.Station.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Research.TechnologyDisk.Systems;

public sealed class DiskConsoleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedResearchSystem _sharedResearch = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    // Frontier
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    // Frontier - end

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DiskConsoleComponent, ComponentInit>(OnComponentInit); // Frontier
        SubscribeLocalEvent<DiskConsoleComponent, ComponentRemove>(OnComponentRemove); // Frontier
        SubscribeLocalEvent<DiskConsoleComponent, DiskConsolePrintDiskMessage>(OnPrintDisk);
        SubscribeLocalEvent<DiskConsoleComponent, DiskConsolePrintRareDiskMessage>(OnPrintRareDisk); // Frontier
        SubscribeLocalEvent<DiskConsoleComponent, DiskConsoleEjectResearchMessage>(OnEjectResearchButton); // Frontier
        SubscribeLocalEvent<DiskConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DiskConsoleComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<DiskConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);

        SubscribeLocalEvent<DiskConsolePrintingComponent, ComponentShutdown>(OnShutdown);
    }

    /// <summary>
    /// Frontier: ID card slot implementation
    /// </summary>
    private void OnComponentInit(EntityUid uid, DiskConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, DiskConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
    }

    /// <summary>
    /// Frontier: ID card slot implementation
    /// </summary>
    private void OnComponentRemove(EntityUid uid, DiskConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.TargetIdSlot);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiskConsolePrintingComponent, DiskConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var printing, out var console, out var xform))
        {
            if (printing.FinishTime > _timing.CurTime)
                continue;

            RemComp(uid, printing);
            if (console.DiskRare) // Frontier
                Spawn(console.DiskPrototypeRare, xform.Coordinates);
            else if (console.DiskAllResearch) // Frontier
            {
                var diskUid = Spawn(console.DiskPrototypeBundled, xform.Coordinates);
                EjectResearchIntoBundle(diskUid, uid);
            }
            else
                Spawn(console.DiskPrototype, xform.Coordinates);
        }
    }

    private void OnPrintDisk(EntityUid uid, DiskConsoleComponent component, DiskConsolePrintDiskMessage args)
    {
        if (HasComp<DiskConsolePrintingComponent>(uid))
            return;

        if (!_research.TryGetClientServer(uid, out var server, out var serverComp))
            return;

        if (serverComp.Points < component.PricePerDisk)
            return;

        _research.ModifyServerPoints(server.Value, -component.PricePerDisk, serverComp);
        _audio.PlayPvs(component.PrintSound, uid);

        var printing = EnsureComp<DiskConsolePrintingComponent>(uid);
        printing.FinishTime = _timing.CurTime + component.PrintDuration;
        component.DiskRare = false;
        UpdateUserInterface(uid, component);
    }

    private void OnPrintRareDisk(EntityUid uid, DiskConsoleComponent component, DiskConsolePrintRareDiskMessage args) // Frontier
    {
        if (HasComp<DiskConsolePrintingComponent>(uid))
            return;

        if (!_research.TryGetClientServer(uid, out var server, out var serverComp))
            return;

        if (serverComp.Points < component.PricePerRareDisk)
            return;

        _research.ModifyServerPoints(server.Value, -component.PricePerRareDisk, serverComp);
        _audio.PlayPvs(component.PrintSound, uid);

        var printing = EnsureComp<DiskConsolePrintingComponent>(uid);
        printing.FinishTime = _timing.CurTime + component.PrintDuration;
        component.DiskRare = true;
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Frontier: Ejects all research if captains id card is inside the console.
    /// This function ejects all research and does not carry over any remaining research points.
    /// </summary>
    private void OnEjectResearchButton(EntityUid consoleUid,
        DiskConsoleComponent component,
        DiskConsoleEjectResearchMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            _popup.PopupEntity(Loc.GetString("tech-disk-console-no-idcard"), consoleUid);
            _audio.PlayEntity(component.ErrorSound, player, consoleUid);
            return;
        }

        if (!_entityManager.TryGetComponent<ShuttleDeedComponent>(targetId, out var deedComponent))
        {
            _popup.PopupEntity(Loc.GetString("tech-disk-console-invalid-idcard"), consoleUid);
            _audio.PlayEntity(component.ErrorSound, player, consoleUid);
            return;
        }

        var stationUid = _station.GetOwningStation(consoleUid);
        if (stationUid == null || !_entityManager.TryGetComponent<TechnologyDatabaseComponent>(stationUid, out var databaseComponent))
        {
            _popup.PopupEntity(Loc.GetString("tech-disk-console-no-server"), consoleUid);
            _audio.PlayEntity(component.ErrorSound, player, consoleUid);
            return;
        }

        if (databaseComponent.UnlockedTechnologies.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("tech-disk-console-nothing-to-eject"), consoleUid);
            _audio.PlayEntity(component.ErrorSound, player, consoleUid);
            return;
        }

        _audio.PlayPvs(component.PrintSound, consoleUid);
        var printing = EnsureComp<DiskConsolePrintingComponent>(consoleUid);
        printing.FinishTime = _timing.CurTime + component.PrintDuration;
        component.DiskRare = false;
        component.DiskAllResearch = true;
        UpdateUserInterface(consoleUid, component);
    }

    /// <summary>
    /// Moves all research from the console to the disk and erases source research.
    /// </summary>
    /// <param name="diskUid"></param>
    /// <param name="consoleUid"></param>
    private void EjectResearchIntoBundle(
        EntityUid diskUid,
        EntityUid consoleUid)
    {
        if (!_entityManager.TryGetComponent<DiskConsoleComponent>(consoleUid, out var component) ||
            !_research.TryGetClientServer(consoleUid, out var server, out var serverComp) ||
            !_entityManager.TryGetComponent<TechnologyDatabaseComponent>(server.Value, out var databaseComponent))
            return;

        var targetDatabase = _entityManager.EnsureComponent<TechnologyDatabaseComponent>(diskUid);
        serverComp.Points = 0;
        _sharedResearch.MoveResearch(databaseComponent, targetDatabase);

        // Finally, update the UI to reflect the change in points
        UpdateUserInterface(consoleUid, component);
    }

    private void OnPointsChanged(EntityUid uid, DiskConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnRegistrationChanged(EntityUid uid, DiskConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnBeforeUiOpen(EntityUid uid, DiskConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    public void UpdateUserInterface(EntityUid uid, DiskConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var totalPoints = 0;
        if (_research.TryGetClientServer(uid, out _, out var server))
        {
            totalPoints = server.Points;
        }

        var canPrint = !(TryComp<DiskConsolePrintingComponent>(uid, out var printing) && printing.FinishTime >= _timing.CurTime) &&
                       totalPoints >= component.PricePerDisk;

        var canPrintRare = !(TryComp<DiskConsolePrintingComponent>(uid, out var printingRare) && printingRare.FinishTime >= _timing.CurTime) &&
                       totalPoints >= component.PricePerRareDisk;

        var idInside = component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true };
        var canPrintAllResearch = !(TryComp<DiskConsolePrintingComponent>(uid, out var printingAllResearch) && printingAllResearch.FinishTime >= _timing.CurTime) && idInside;

        var state = new DiskConsoleBoundUserInterfaceState(totalPoints, component.PricePerDisk, component.PricePerRareDisk, canPrint, canPrintRare, canPrintAllResearch);
        _ui.SetUiState(uid, DiskConsoleUiKey.Key, state);
    }

    private void OnShutdown(EntityUid uid, DiskConsolePrintingComponent component, ComponentShutdown args)
    {
        UpdateUserInterface(uid);
    }
}

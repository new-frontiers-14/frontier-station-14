using System.Collections.ObjectModel;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Components;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using System.Linq;
using Content.Shared.Roles;

namespace Content.Server.StationRecords.Systems;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordRemovedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AdjustStationJobMsg>(OnAdjustJob);

        Subs.BuiEvents<GeneralStationRecordConsoleComponent>(GeneralStationRecordConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
        });
    }

    private void UpdateUserInterface<T>(Entity<GeneralStationRecordConsoleComponent> ent, ref T args)
    {
        UpdateUserInterface(ent);
    }

    // TODO: instead of copy paste shitcode for each record console, have a shared records console comp they all use
    // then have this somehow play nicely with creating ui state
    // if that gets done put it in StationRecordsSystem console helpers section :)
    private void OnKeySelected(Entity<GeneralStationRecordConsoleComponent> ent, ref SelectStationRecord msg)
    {
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
    }

    private void OnAdjustJob(EntityUid uid, GeneralStationRecordConsoleComponent component, AdjustStationJobMsg msg)
    {
        var stationUid = _stationSystem.GetOwningStation(uid);
        if (stationUid is EntityUid station)
        {
            _stationJobsSystem.TryAdjustJobSlot(station, msg.JobProto, msg.Amount, false, true);
        }
        UpdateUserInterface((uid,component));
    }
    private void OnFiltersChanged(Entity<GeneralStationRecordConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent);
        }
    }

    private void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            _ui.SetUiState(uid, GeneralStationRecordConsoleKey.Key, new GeneralStationRecordConsoleState());
            return;
        }

        var jobList = _stationJobsSystem.GetJobs(owningStation.Value);

        var listing = _stationRecords.BuildListing((owningStation.Value, stationRecords), console.Filter);

        switch (listing.Count)
        {
            case 0:
                GeneralStationRecordConsoleState emptyState = new(null, null, null, jobList, console.Filter);
                _ui.SetUiState(uid, GeneralStationRecordConsoleKey.Key, emptyState);
                return;
            case 1:
                console.ActiveKey = listing.Keys.First();
                break;
        }

        if (console.ActiveKey is not { } id)
            return;

        var key = new StationRecordKey(id, owningStation.Value);
        _stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record, stationRecords);

        GeneralStationRecordConsoleState newState = new(id, record, listing, jobList, console.Filter);
        _ui.SetUiState(uid, GeneralStationRecordConsoleKey.Key, newState);
    }
}

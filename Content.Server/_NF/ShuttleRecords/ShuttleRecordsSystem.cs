using System.Diagnostics.CodeAnalysis;
using Content.Server._NF.SectorServices;
using Content.Server._NF.ShuttleRecords.Components;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared._NF.ShuttleRecords;
using Content.Shared.Access.Systems;
using Content.Shared._NF.Shipyard.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._NF.ShuttleRecords;

public sealed partial class ShuttleRecordsSystem : SharedShuttleRecordsSystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;


    public override void Initialize()
    {
        base.Initialize();
        InitializeShuttleRecords();
    }

    /**
     * Adds a record to the shuttle records list.
     * <param name="record">The record to add.</param>
     */
    public void AddRecord(ShuttleRecord record)
    {
        if (!TryGetShuttleRecordsDataComponent(out var component))
            return;

        record.TimeOfPurchase = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        component.ShuttleRecords[record.EntityUid] = record;
        RefreshStateForAll();
    }

    /**
     * Edits an existing record if one exists for the entity given in the Record
     * <param name="record">The record to update.</param>
     */
    public void TryUpdateRecord(ShuttleRecord record)
    {
        if (!TryGetShuttleRecordsDataComponent(out var component))
            return;

        component.ShuttleRecords[record.EntityUid] = record;
        RefreshStateForAll();
    }

    /**
     * Edits an existing record if one exists for the given entity
     * <param name="record">The record to add.</param>
     */
    public bool TryGetRecord(NetEntity uid, [NotNullWhen(true)] out ShuttleRecord? record)
    {
        if (!TryGetShuttleRecordsDataComponent(out var component) ||
            !component.ShuttleRecords.ContainsKey(uid))
        {
            record = null;
            return false;
        }

        record = component.ShuttleRecords[uid];
        return true;
    }

    private bool TryGetShuttleRecordsDataComponent([NotNullWhen(true)] out SectorShuttleRecordsComponent? component)
    {
        if (_entityManager.EnsureComponent<SectorShuttleRecordsComponent>(
                uid: _sectorService.GetServiceEntity(),
                out var shuttleRecordsComponent))
        {
            component = shuttleRecordsComponent;
            return true;
        }

        component = null;
        return false;
    }
}

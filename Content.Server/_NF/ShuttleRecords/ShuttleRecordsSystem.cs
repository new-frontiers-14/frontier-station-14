using System.Diagnostics.CodeAnalysis;
using Content.Server._NF.SectorServices;
using Content.Server._NF.ShuttleRecords.Components;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared._NF.ShuttleRecords;
using Content.Shared.Access.Systems;
using Robust.Server.GameObjects;

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

        component.ShuttleRecordsList.Add(record);
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

using System.Diagnostics.CodeAnalysis;
using Content.Server._NF.ShuttleRecords.Components;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared._NF.ShuttleRecords;
using Robust.Server.GameObjects;

namespace Content.Server._NF.ShuttleRecords;

public sealed partial class ShuttleRecordsSystem: SharedShuttleRecordsSystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
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
     * <param name="anyGridEntityUid">The entityUid of any grid entity to which we should add the record to.</param>
     */
    public void AddRecord(ShuttleRecord record, EntityUid anyGridEntityUid)
    {
        if (!TryGetShuttleRecordsDataComponent(anyGridEntityUid, out var component))
            return;

        component.ShuttleRecordsList.Add(record);
    }

    private bool TryGetShuttleRecordsDataComponent(EntityUid consoleEntityUid, [NotNullWhen(true)] out ShuttleRecordsDataComponent? component)
    {
        var station = _station.GetOwningStation(consoleEntityUid);
        if (station is null)
        {
            component = null;
            return false;
        }

        _entityManager.EnsureComponent<ShuttleRecordsDataComponent>(station.Value, out component);
        return true;
    }
}

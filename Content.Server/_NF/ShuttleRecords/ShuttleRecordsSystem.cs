using System.Diagnostics.CodeAnalysis;
using Content.Server._NF.SectorServices;
using Content.Server._NF.ShuttleRecords.Components;
using Content.Shared._NF.ShuttleRecords;

namespace Content.Server._NF.ShuttleRecords;

public sealed partial class ShuttleRecordsSystem : SharedShuttleRecordsSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;


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

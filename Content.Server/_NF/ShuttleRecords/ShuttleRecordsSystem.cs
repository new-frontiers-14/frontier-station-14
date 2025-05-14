using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using Robust.Shared.Toolshed.Commands.Values;
using Content.Server.StationRecords.Systems;
using Microsoft.CodeAnalysis.Elfie.Extensions;

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
        record.OriginalName = record.Name;
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

    public bool TrySetSaleTime(NetEntity uid)
    {
        if (TryGetRecord(uid, out var record))
        {
            record.TimeOfSale = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
            TryUpdateRecord(record);
            return true;
        }
        return false;
    }

    public string? GetStatsPrintout(out byte[]? rawData)
    {
        rawData = null;
        if (!TryGetShuttleRecordsDataComponent(out var records))
        {
            return null;
        }

        StringBuilder builder = new();
        Dictionary<String, RecordSummary> shipTypes = new(); // committing crimes against structs here
        var totalShips = 0;
        var totalAbandoned = 0;
        List<TimeSpan> totalLifetimes = new();

        // sort through the records and use originalname to categorise ships
        foreach (var record in records.ShuttleRecords.Values)
        {
            if (record.OriginalName is null)
                continue;

            if (!shipTypes.ContainsKey(record.OriginalName))
                shipTypes.Add(record.OriginalName, new RecordSummary());

            if (shipTypes.TryGetValue(record.OriginalName, out var value))
            {
                value.Count += 1;
                totalShips += 1;

                if (EntityManager.TryGetEntity(record.EntityUid, out _)) // check if the ship still exists
                {
                    value.AbandonedCount += 1;
                    totalAbandoned += 1;
                }

                if (record.TimeOfPurchase is not null && record.TimeOfSale is not null)
                {
                    var lifetime = record.TimeOfSale.Value.Subtract(record.TimeOfPurchase.Value);
                    value.Lifetimes.Add(lifetime);
                    totalLifetimes.Add(lifetime);
                }
            }
        }

        var sortedSummaries = shipTypes.OrderByDescending(record => record.Value.Count).ThenBy(record => record.Key);

        // export raw data as a file for discord

        rawData = JsonSerializer.SerializeToUtf8Bytes(shipTypes, new JsonSerializerOptions { WriteIndented = true });

        /* eventual discord message should be of the format
        ```
         Num │ Abnd │ Avg time │ Type
        ─────┼──────┼──────────┼───────────
        1234 │ 1234 │    00:00 │ NAME
        1234 │ 1234 │    00:00 │ NAME
        1234 │ 1234 │    00:00 │ NAME
        ─────┼──────┼──────────┼───────────
        1234 │ 1234 │    00:00 │
        ```
        */

        builder.AppendLine("```");
        builder.AppendLine(" Num │ Abnd │ Avg time │ Type");
        builder.AppendLine("─────┼──────┼──────────┼───────────");
        foreach (var record in sortedSummaries)
        {
            // fallback, in case every ship of this type was abandoned this round and there are no lifetimes to report
            var averageLifetime = "N/A";
            if (record.Value.Lifetimes.Count != 0)
            {
                averageLifetime = TimeSpan.FromSeconds(record.Value.Lifetimes.Average(timeSpan => timeSpan.TotalSeconds)).ToString(@"hh\:mm");
            }

            // pad data for formatting
            var num = record.Value.Count.ToString().PadLeft(4);
            var abandoned = record.Value.AbandonedCount.ToString().PadLeft(5);
            var avgTime = averageLifetime.PadLeft(9);

            builder.AppendLine($"{num} │{abandoned} │{avgTime} │ {record.Key}");
        }

        builder.AppendLine("─────┼──────┼──────────┼───────────");

        // fallback, in case somehow every single ship was abandoned this round and there are no lifetimes to report
        var totalAvgLifetime = "N/A";
        totalAvgLifetime = TimeSpan.FromSeconds(totalLifetimes.Average(timeSpan => timeSpan.TotalSeconds)).ToString(@"hh\:mm");
        builder.AppendLine($"{totalShips.ToString().PadLeft(4)} │{totalAbandoned.ToString().PadLeft(5)} │{totalAvgLifetime.PadLeft(9)} │");
        builder.AppendLine("```");
        return builder.ToString();
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
    private class RecordSummary()
    {
        public int Count = 0;
        public int AbandonedCount = 0;
        public List<TimeSpan> Lifetimes = new();
    }
}


using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using System.Text.Json;
using Content.Server._NF.SectorServices;
using Content.Server._NF.ShuttleRecords.Components;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared._NF.ShuttleRecords;
using Content.Shared.Access.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Server;


namespace Content.Server._NF.ShuttleRecords;

public sealed partial class ShuttleRecordsSystem : SharedShuttleRecordsSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;


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

    public (string, byte[])? GetStatsPrintout()
    {
        if (!TryGetShuttleRecordsDataComponent(out var records))
        {
            return null;
        }

        StringBuilder builder = new();
        Dictionary<string, RecordSummary> shipTypes = new(); // committing crimes against structs here
        var totalShips = 0;
        var totalAbandoned = 0;
        List<TimeSpan> totalLifetimes = new();

        // sort through the records and use VesselPrototypeId to categorise ships
        foreach (var record in records.ShuttleRecords.Values)
        {
            if (record.VesselPrototypeId is null)
                continue;

            if (!shipTypes.ContainsKey(record.VesselPrototypeId))
                shipTypes.Add(record.VesselPrototypeId, new RecordSummary());

            if (shipTypes.TryGetValue(record.VesselPrototypeId, out var value))
            {
                value.Count += 1;
                totalShips += 1;

                if (EntityManager.TryGetEntity(record.EntityUid, out _)) // check if the ship still exists
                {
                    value.AbandonedCount += 1;
                    totalAbandoned += 1;
                }

                if (record.TimeOfPurchase is { } purchaseTime && record.TimeOfSale is { } saleTime)
                {
                    var lifetime = saleTime.Subtract(purchaseTime);
                    value.Lifetimes.Add(lifetime);
                    totalLifetimes.Add(lifetime);
                }
            }
        }

        var sortedSummaries = shipTypes.OrderByDescending(record => record.Value.Count).ThenBy(record => record.Key);

        // export raw data as a file for discord

        var rawData = JsonSerializer.SerializeToUtf8Bytes(new ShuttleStatisticsFile(
            serverName: _baseServer.ServerName,
            roundId: _gameTicker.RoundId,
            shuttles: shipTypes
        ), new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });

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
            builder.AppendLine($"{record.Value.Count,4} │ {record.Value.AbandonedCount,4} │{averageLifetime,9} │ {record.Key}");
        }

        builder.AppendLine("─────┼──────┼──────────┼───────────");

        // fallback, in case somehow every single ship was abandoned this round and there are no lifetimes to report
        var totalAvgLifetime = "N/A";
        if (totalLifetimes.Count > 0)
            totalAvgLifetime = TimeSpan.FromSeconds(totalLifetimes.Average(timeSpan => timeSpan.TotalSeconds)).ToString(@"hh\:mm");
        builder.AppendLine($"{totalShips.ToString(),4} │ {totalAbandoned.ToString(),4} │{totalAvgLifetime,9} │");
        builder.AppendLine("```");
        return (builder.ToString(), rawData);
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

    private sealed class ShuttleStatisticsFile(
        string serverName,
        int roundId,
        Dictionary<string, RecordSummary> shuttles
    )
    {
        /// <summary>
        /// Hardcoded version. Bump it when we make changes.
        /// </summary>
        public readonly int Version = 1;
        public string ServerName = serverName;
        public int RoundId = roundId;
        public Dictionary<string, RecordSummary> Shuttles = shuttles;
    }

    private sealed class RecordSummary()
    {
        public int Count = 0;
        public int AbandonedCount = 0;
        public List<TimeSpan> Lifetimes = new();
    }
}


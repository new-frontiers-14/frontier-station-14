using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Server.Paper;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Shared.Enums;

namespace Content.Server.Fax;

public sealed class ShipyardRecordPaperSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;

    [Dependency] private readonly IGameTiming _gameTiming = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipyardRecordPaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShipyardRecordPaperComponent, ShipyardRecordPaperTransmitEvent>(SendShuttleRecords);
    }

    private void OnMapInit(EntityUid uid, ShipyardRecordPaperComponent component, MapInitEvent args)
    {
        SetupPaper(uid, component);
    }

    private void SetupPaper(EntityUid uid, ShipyardRecordPaperComponent? component = null, EntityUid? station = null)
    {
        if (!Resolve(uid, ref component))
            return;


        _paper.SetContent(uid, "paperContent");

    }

    /// <summary>
    ///     Send a fax to each fax machine with details regarding purchase and ownership of a vessel
    /// </summary>
    private void SendShuttleRecords(EntityUid faxEnt, ShipyardRecordPaperComponent recordPaper, ShipyardRecordPaperTransmitEvent args)
    {
        var currentTime = _gameTiming.RealTime;


        TryComp<FaxMachineComponent>(faxEnt, out var fax);
        var printout = new FaxPrintout(
        Loc.GetString("shipyard-record-paper-content", ("vessel_name", args.VesselName), ("vessel_owner_name", args.VesselOwnerName), ("vessel_owner_species", args.VesselOwnerSpecies), ("vessel_owner_gender", args.VesselOwnerGender), ("vessel_owner_age", args.VesselOwnerAge), ("vessel_owner_fingerprints", args.VesselOwnerFingerprints), ("vessel_owner_dna", args.VesselOwnerDNA), ("time", currentTime)),
        Loc.GetString("shipyard-record-paper-name", ("vessel_name", args.VesselName), ("time", currentTime)),
        "PaperShipyardRecordBase",
        null);
        _faxSystem.Receive(faxEnt, printout, null, fax);
    }

}

[ByRefEvent]
public readonly record struct ShipyardRecordPaperTransmitEvent(string VesselName, string VesselOwnerName, string VesselOwnerSpecies, Gender VesselOwnerGender, int VesselOwnerAge, string VesselOwnerFingerprints, string VesselOwnerDNA);

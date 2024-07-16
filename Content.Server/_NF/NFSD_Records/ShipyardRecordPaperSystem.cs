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
using Content.Shared.Fax.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Content.Shared.Preferences;
using Content.Shared.Shipyard.Prototypes;
using Robust.Shared.Prototypes;
using System.Net.NetworkInformation;

namespace Content.Server.Fax;

public sealed class ShipyardRecordPaperSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly IRobustRandom _robustRandom = default!;


    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<ShipyardRecordPaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShipyardRecordPaperComponent, ShipyardRecordPaperTransmitEvent>(SendShuttleRecords);
    }

    /*private void OnMapInit(EntityUid uid, ShipyardRecordPaperComponent component, MapInitEvent args)
    {
        SetupPaper(uid, component);
    }*/

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
        var currentTime = _gameTiming.RealTime.ToString(@"hh\:mm\:ss");

        //Sets record paper type based on vessel's class
        var shipyardRecordPaperPrototype = "PaperShipyardRecordBase";
        switch(args.VesselClass)
        {
            case "Undetermined":
                shipyardRecordPaperPrototype = "PaperShipyardRecordBase";
            break;
            case "Civilian":
                shipyardRecordPaperPrototype = "PaperShipyardRecordCivilian";
            break;
            case "Expedition":
                shipyardRecordPaperPrototype = "PaperShipyardRecordExpedition";
            break;
            case "Medical":
                shipyardRecordPaperPrototype = "PaperShipyardRecordMedical";
            break;
            case "Blackmarket":
                shipyardRecordPaperPrototype = "PaperShipyardRecordBlackmarket";
            break;
            case "NFSD":
                shipyardRecordPaperPrototype = "PaperShipyardRecordNFSD";
            break;
            case "Scrap":
                shipyardRecordPaperPrototype = "PaperShipyardRecordScrap";
            break;
            case "Science":
                shipyardRecordPaperPrototype = "PaperShipyardRecordScience";
            break;
            case "Service":
                shipyardRecordPaperPrototype = "PaperShipyardRecordService";
            break;
            case "Engineering":
                shipyardRecordPaperPrototype = "PaperShipyardRecordEngineering";
            break;
            case "Syndicate":
                shipyardRecordPaperPrototype = "PaperShipyardRecordSyndicate";
            break;
            case "Cargo":
                shipyardRecordPaperPrototype = "PaperShipyardRecordCargo";
            break;
            case "Salvage":
                shipyardRecordPaperPrototype = "PaperShipyardRecordSalvage";
            break;
            default:
                shipyardRecordPaperPrototype = "PaperShipyardRecordBase";
            break;
        }

    //check if vessel's class is Blackmarket or Syndicate, If yes then scramble
     if(args.VesselClass == "Blackmarket" || args.VesselClass == "Syndicate")
        {
            //generate random person
            HumanoidCharacterProfile fakeOwner = HumanoidCharacterProfile.Random();


            //Gets information from all vessels into lists
            List<string> vesselNameList = new List<string>();
            List<string> vesselCategoryList = new List<string>();
            List<string> vesselDescriptionList = new List<string>();
            List<string> vesselPriceList = new List<string>();
            foreach (var vessel in _prototypeManager.EnumeratePrototypes<VesselPrototype>())
            {
                //Saves information into lists
                vesselNameList.Add(vessel.Name);
                vesselCategoryList.Add(vessel.Category);
                vesselDescriptionList.Add(vessel.Description);
                vesselPriceList.Add(vessel.Price.ToString());
            }
            //Converts lists into arrays
            string[] vesselName = vesselNameList.ToArray();
            string[] vesselCategory = vesselCategoryList.ToArray();
            string[] vesselDescription = vesselDescriptionList.ToArray();
            string[] vesselPrice = vesselPriceList.ToArray();

            //Chooses random vessel which information will be utilised
            var ran_int = _robustRandom.Next(0, vesselName.Length);

            //Combines real and fake information to then scramble it
            string scramVesselOwnerName = args.VesselOwnerName + fakeOwner.Name;
            string scramVesselOwnerSpecies = args.VesselOwnerSpecies + fakeOwner.Species;

            string scramVesselName = args.VesselName + vesselName[ran_int];
            string scramVesselCategory = args.VesselCategory + vesselCategory[ran_int];
            string scramVesselDescription = args.VesselDescription + vesselDescription[ran_int];

            //Given with just two prices it was too easy for players to metagame, additional 2 numbers will be added to make it bit harder
            int randPriceValue = _robustRandom.Next(10, 99);
            string scramVesselPrice = args.VesselPrice + randPriceValue + vesselPrice[ran_int];

            //save elements that can be properly scrambled
            string[] elements = {scramVesselName, scramVesselOwnerName, scramVesselOwnerSpecies, args.VesselOwnerFingerprints, args.VesselOwnerDNA, scramVesselCategory, scramVesselDescription, scramVesselPrice};

            //Scrambles each element
            for(int ii = 0; ii < elements.Length; ii++)
            {
                string input = elements[ii];
                char[] chars = input.ToCharArray();
                for (int i = 0; i < chars.Length; i++)
                {
                    int randomIndex = _robustRandom.Next(0, chars.Length);
                    char temp = chars[randomIndex];
                    chars[randomIndex] = chars[i];
                    chars[i] = temp;
                }
                string scrambled = new string(chars);
                elements[ii] = scrambled;
            }


            TryComp<FaxMachineComponent>(faxEnt, out var fax);
            var printout = new FaxPrintout(
            Loc.GetString("shipyard-record-paper-content", ("vessel_name", elements[0]), ("vessel_owner_name", elements[1]), ("vessel_owner_species", elements[2]), ("vessel_owner_gender", fakeOwner.Gender), ("vessel_owner_age", fakeOwner.Age), ("vessel_owner_fingerprints", elements[3]), ("vessel_owner_dna", elements[4]), ("time", currentTime), ("vessel_category", elements[5]), ("vessel_class", args.VesselClass), ("vessel_group", args.VesselGroup), ("vessel_price", elements[7]), ("vessel_description", elements[6])),
            Loc.GetString("shipyard-record-paper-name", ("vessel_name", elements[0]), ("time", currentTime)),
            null,
            shipyardRecordPaperPrototype,
            null);
            _faxSystem.Receive(faxEnt, printout, null, fax);


       } else {

            TryComp<FaxMachineComponent>(faxEnt, out var fax);
            var printout = new FaxPrintout(
            Loc.GetString("shipyard-record-paper-content", ("vessel_name", args.VesselName), ("vessel_owner_name", args.VesselOwnerName), ("vessel_owner_species", args.VesselOwnerSpecies), ("vessel_owner_gender", args.VesselOwnerGender), ("vessel_owner_age", args.VesselOwnerAge), ("vessel_owner_fingerprints", args.VesselOwnerFingerprints), ("vessel_owner_dna", args.VesselOwnerDNA), ("time", currentTime), ("vessel_category", args.VesselCategory), ("vessel_class", args.VesselClass), ("vessel_group", args.VesselGroup), ("vessel_price", args.VesselPrice), ("vessel_description", args.VesselDescription) ),
            Loc.GetString("shipyard-record-paper-name", ("vessel_name", args.VesselName), ("time", currentTime)),
            null,
            shipyardRecordPaperPrototype,
            null);
            _faxSystem.Receive(faxEnt, printout, null, fax);
       }
    }

}

[ByRefEvent]
public readonly record struct ShipyardRecordPaperTransmitEvent(string VesselName, string VesselOwnerName, string VesselOwnerSpecies, Gender VesselOwnerGender, int VesselOwnerAge, string VesselOwnerFingerprints, string VesselOwnerDNA, string VesselCategory, string VesselClass, string VesselGroup, int VesselPrice, string VesselDescription);

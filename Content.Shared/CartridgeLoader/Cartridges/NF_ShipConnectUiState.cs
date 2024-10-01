using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

using Robust.Shared.Prototypes;

using Content.Shared.Paper;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Shared.Enums;
using Content.Shared.Preferences;
using Content.Shared.Shipyard.Prototypes;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NF_ShipConnectUiState : BoundUserInterfaceState
{
    public NF_ShipConnectUiState(string vessel_name, string vessel_owner_name, string vessel_owner_species, Gender vessel_owner_gender, int vessel_owner_age, string vessel_owner_fingerprints, string vessel_owner_dna, string vessel_category, string vessel_class, string vessel_group, int vessel_price, string vessel_description)
    {
        VesselName = vessel_name;
        VesselOwnerName = vessel_owner_name;
        VesselOwnerSpecies = vessel_owner_species;
        VesselOwnerGender = vessel_owner_gender;
        VesselOwnerAge = vessel_owner_age;
        VesselOwnerFingerprints = vessel_owner_fingerprints;
        VesselOwnerDNA = vessel_owner_dna;
        VesselCategory = vessel_category;
        VesselClass = vessel_class;
        VesselGroup = vessel_group;
        VesselPrice = vessel_price;
        VesselDescription = vessel_description;
    }

    [DataField]
    public string VesselName;

    [DataField]
    public string VesselOwnerName;

    [DataField]
    public string VesselOwnerSpecies;

    [DataField]
    public Gender VesselOwnerGender;

    [DataField]
    public int VesselOwnerAge;

    [DataField]
    public string VesselOwnerFingerprints;

    [DataField]
    public string VesselOwnerDNA;

    [DataField]
    public string VesselCategory;

    [DataField]
    public string VesselClass;

    [DataField]
    public string VesselGroup;

    [DataField]
    public int VesselPrice;

    [DataField]
    public string VesselDescription;

}

[Serializable, NetSerializable]
public sealed class NF_ShipConnectSyncMessageEvent : CartridgeMessageEvent
{
    public NF_ShipConnectSyncMessageEvent()
    { }
}

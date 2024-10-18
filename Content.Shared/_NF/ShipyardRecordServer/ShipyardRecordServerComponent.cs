using Robust.Shared.Prototypes;

using Content.Shared.Paper;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Shared.Enums;
using Content.Shared.Preferences;
using Content.Shared.Shipyard.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ShipyardRecordServer;

[RegisterComponent]
public sealed partial class ShipyardRecordsServerComponent : Component {
  [DataField]
  public List<RecordEntry> Records = new();

  [Serializable, DataDefinition]
  public sealed partial class RecordEntry {
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
}

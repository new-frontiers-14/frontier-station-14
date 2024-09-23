using Robust.Shared.Prototypes;
using Content.Shared._NF.Smuggling.Prototypes;

namespace Content.Server._NF.Smuggling.Components;

/// <summary>
///     Denotes a station as one that will report, to a given radio channel, 
///
///     When a dead drop on the station is compromised, another
///     potential dead drop is selected instead.
/// </summary>
[RegisterComponent]
public sealed partial class StationDeadDropReportingComponent : Component
{
    [DataField(required: true)]
    public ProtoId<SmugglingReportMessageSetPrototype> MessageSet;
}

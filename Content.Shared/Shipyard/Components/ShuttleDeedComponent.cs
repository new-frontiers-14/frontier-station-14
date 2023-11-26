using Content.Shared.Shipyard;

namespace Content.Shared.Shipyard.Components;

/// <summary>
/// Tied to an ID card when a ship is purchased. 1 ship per captain.
/// </summary>
[RegisterComponent, Access(typeof(SharedShipyardSystem))]
public sealed partial class ShuttleDeedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("shuttleUid")]
    public EntityUid? ShuttleUid;

    [ViewVariables(VVAccess.ReadWrite), DataField("shuttleName")]
    public string? ShuttleName = "Unknown";

    [ViewVariables(VVAccess.ReadWrite), DataField("shuttleOwner")]
    public EntityUid? ShuttleOwner = "Unknown";
}

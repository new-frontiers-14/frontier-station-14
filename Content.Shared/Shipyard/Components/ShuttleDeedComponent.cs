using Content.Shared.Shipyard;

namespace Content.Shared.Shipyard.Components;

/// <summary>
/// Tied to an ID card when a ship is purchased. 1 ship per captain.
/// </summary>
[RegisterComponent, Access(typeof(SharedShipyardSystem))]
public sealed partial class ShuttleDeedComponent : Component
{
    [DataField("shuttleUid")]
    public EntityUid? ShuttleUid;

    [DataField("shuttleName")]
    public string? ShuttleName;

    [DataField("shuttleOwner")]
    public EntityUid? ShuttleOwner = null;
}

using Robust.Shared.GameStates;

namespace Content.Shared.Shipyard.Components;

/// <summary>
/// Tied to an ID card when a ship is purchased. 1 ship per captain.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedShipyardSystem))]
public sealed partial class ShuttleDeedComponent : Component
{
    public const int MaxNameLength = 30;
    public const int MaxSuffixLength = 3 + 1 + 4; // 3 digits, dash, up to 4 letters - should be enough

    [DataField]
    public EntityUid? ShuttleUid = null;

    [DataField]
    public string? ShuttleName = "Unknown";

    [DataField("shuttleSuffix")]
    public string? ShuttleNameSuffix;

    [DataField]
    public string? ShuttleOwner = "Unknown";
}

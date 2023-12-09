namespace Content.Shared.Shipyard.Components;

/// <summary>
/// Tied to an ID card when a ship is purchased. 1 ship per captain.
/// </summary>
[RegisterComponent, Access(typeof(SharedShipyardSystem))]
public sealed partial class ShuttleDeedComponent : Component
{
    public const int MaxNameLength = 30;
    public const int MaxSuffixLength = 3 + 1 + 4; // 3 digits, dash, up to 4 letters - should be enough

    [DataField("shuttleUid")]
    public EntityUid? ShuttleUid;

    [DataField("shuttleName")]
    public string? ShuttleName;

    [DataField("shuttleSuffix")]
    public string? ShuttleNameSuffix;

    [DataField("shuttleOwner")]
    public EntityUid? ShuttleOwner;
}

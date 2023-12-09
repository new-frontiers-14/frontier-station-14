using Content.Shared.Inventory;

namespace Content.Server.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent]
public sealed partial class ContainerMagnetPickupComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    public TimeSpan NextScan = TimeSpan.Zero;

    /// <summary>
    /// Is magnet active, when container is anchored?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("pickupWhenAnchored")]
    public bool PickupWhenAnchored = true;

    /// <summary>
    /// Is magnet active, when container is not anchored?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("pickupWhenNotAnchored")]
    public bool PickupWhenNotAnchored = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1f;
}

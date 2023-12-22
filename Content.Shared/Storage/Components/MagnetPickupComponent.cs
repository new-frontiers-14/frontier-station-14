using Content.Shared.Inventory;

namespace Content.Server.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent]
public sealed partial class MagnetPickupComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    public TimeSpan NextScan = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1f;

    /// <summary>
    /// Whether the magnet is attached to a fixture (e.g. ore box) or not (e.g. ore bag)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("isFixture")]
    public bool IsFixture = false;

    /// <summary>
    /// What container slot the magnet needs to be in to work (if not a fixture)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("slotFlags")]
    public SlotFlags SlotFlags = SlotFlags.BELT;

    /// <summary>
    /// Is magnet active, when fixture is anchored?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("pickupWhenAnchored")]
    public bool PickupWhenAnchored = true;

    /// <summary>
    /// Is magnet active, when fixture is not anchored?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("pickupWhenNotAnchored")]
    public bool PickupWhenNotAnchored = true;
}

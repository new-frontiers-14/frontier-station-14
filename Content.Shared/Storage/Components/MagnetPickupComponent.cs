using Content.Shared.Inventory;
<<<<<<< HEAD
using Robust.Shared.GameStates; // Frontier
=======
using Robust.Shared.GameStates;
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8

namespace Content.Shared.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
<<<<<<< HEAD
[RegisterComponent, AutoGenerateComponentPause]
[NetworkedComponent, AutoGenerateComponentState] // Frontier
=======
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[AutoGenerateComponentPause]
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8
public sealed partial class MagnetPickupComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    [AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan NextScan = TimeSpan.Zero;

    /// <summary>
    /// What container slot the magnet needs to be in to work.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("slotFlags")]
    public SlotFlags SlotFlags = SlotFlags.BELT;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1f;

    // Frontier: togglable magnets
    /// <summary>
    /// Is the magnet currently enabled?
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
    public bool MagnetEnabled = true;

    /// <summary>
    /// Is the magnet currently enabled?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool MagnetCanBeEnabled = true;

    /// <summary>
    /// Is the magnet currently enabled?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int MagnetTogglePriority = 3;
    // End Frontier: togglable magnets
}

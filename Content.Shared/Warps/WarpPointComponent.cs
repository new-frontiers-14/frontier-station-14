using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Warps;

/// <summary>
/// Allows ghosts etc to warp to this entity by name.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WarpPointComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public string? Location;

    /// <summary>
    /// If true, ghosts warping to this entity will begin following it.
    /// </summary>
    [DataField]
    public bool Follow;

    /// <summary>
    /// What points should be excluded?
    /// Useful where you want things like a ghost to reach only like CentComm
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    // Frontier: extra fields
    /// <summary>
    /// If true, will sync warp point name with a station/grid name.
    /// </summary>
    [DataField]
    public bool UseStationName;

    /// <summary>
    /// If true, warp point can only be used by admins
    /// </summary>
    [DataField]
    public bool AdminOnly;

    /// <summary>
    /// If true, will set its own name to the station's on creation.
    /// </summary>
    [DataField]
    public bool QueryStationName;

    /// <summary>
    /// If true, will set its own name to the grid's on creation.
    /// </summary>
    [DataField]
    public bool QueryGridName;
    // End Frontier
}

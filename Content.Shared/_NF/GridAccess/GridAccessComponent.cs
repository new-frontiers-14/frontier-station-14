using Robust.Shared.GameStates;

namespace Content.Shared._NF.GridAccess;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GridAccessComponent : Component
{
    /// <summary>
    /// Frontier - Protected grid override
    /// Dictates if this device can be used on protected grids.
    /// </summary>
    [DataField("protectionOverride"), AutoNetworkedField]
    public bool ProtectionOverride = false;
    /// <summary>
    /// Frontier - Grid access
    /// The uid to which this device is limited to be used on.
    /// </summary>
    [DataField("linkedShuttleUid"), AutoNetworkedField]
    public EntityUid? LinkedShuttleUid = null;
}

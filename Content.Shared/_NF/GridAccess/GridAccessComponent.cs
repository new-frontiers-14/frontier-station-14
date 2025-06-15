using Robust.Shared.GameStates;

namespace Content.Shared._NF.GridAccess;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GridAccessComponent : Component
{
    /// <summary>
    /// Frontier - Grid access
    /// The uid to which this device is limited to be used on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedShuttleUid = null;
}

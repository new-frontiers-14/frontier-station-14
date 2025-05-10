using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
///     When activated artifact will spawn a pair of portals. First - right in artifact, Second - at random point of station.
/// </summary>
[RegisterComponent, Access(typeof(XAEPortalSystem))]
public sealed partial class XAEPortalComponent : Component
{
    /// <summary>
    /// Entity that should be spawned as portal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId PortalProto = "PortalArtifact";

    // Frontier: range limit
    /// <summary>
    /// Maximum range that the target entity should be from the portal, in meters.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxRange = 1000f;
    // End Frontier
}

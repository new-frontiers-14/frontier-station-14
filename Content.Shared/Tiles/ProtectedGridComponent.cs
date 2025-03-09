using Robust.Shared.GameStates;
using Robust.Shared.Audio; // Frontier

namespace Content.Shared.Tiles;

/// <summary>
/// Prevents floor tile updates when attached to a grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProtectedGridComponent : Component
{
    // Frontier: define protection types.
    [DataField]
    public bool PreventFloorRemoval = false;
    [DataField]
    public bool PreventFloorPlacement = false;
    [DataField]
    public bool PreventRCDUse = false;
    [DataField]
    public bool PreventEmpEvents = false;
    [DataField]
    public bool PreventExplosions = false;
    [DataField]
    public bool PreventArtifactTriggers = false;
    [DataField]
    public bool KillHostileMobs = false;

    /// <summary>
    /// The sound made when a hostile mob is killed when entering a protected grid.
    /// </summary>
    [DataField]
    public SoundSpecifier HostileMobKillSound = new SoundPathSpecifier("/Audio/Effects/holy.ogg");
    // End Frontier
}

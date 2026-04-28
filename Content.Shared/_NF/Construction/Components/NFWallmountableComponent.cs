using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Construction.Components;

/// <summary>
/// A component that spawns an entity (intended to be a wallmount) when interacting with a wall.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NFWallmountableComponent : Component
{
    /// <summary>
    /// The amount of time to spend in the doafter.
    /// </summary>
    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The entity to spawn on the wall.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId Spawn;

    /// <summary>
    /// If false, will spawn facing south.
    /// If true, will spawn facing the user.
    /// </summary>
    [DataField]
    public bool RotateToUser;
}

/// <summary>
/// Raised when an entity tries to install an entity with NFWallMountableComponent on a wall.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NFWallmountDoAfterEvent : SimpleDoAfterEvent;

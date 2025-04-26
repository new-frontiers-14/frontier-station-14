using Content.Shared._NF.Roles.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Roles.Components;

/// <summary>
/// This denotes an entity with a job.
/// As opposed to MindRoleComponent, which is for OOC role tracking (by player)
/// This can be used for IC role tracking (by in-game character)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedJobTrackingSystem), typeof(SharedGameTicker))]
public sealed partial class JobTrackingComponent : Component
{
    /// <summary>
    /// The station this entity spawned in on.
    /// If they enter cryo, a slot will be reopened if there are fewer open slots than there are empty default slots.
    /// </summary>
    [DataField(serverOnly: true)]
    public EntityUid SpawnStation;

    /// <summary>
    /// The job this entity holds.  See above.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// If true, this entity is holding an active job slot.
    /// </summary>
    [DataField(serverOnly: true)]
    public bool Active;
}

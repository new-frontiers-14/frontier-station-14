using Content.Server._NF.GameTicking.Systems;
using Content.Server.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.GameRule.Components;

/// <summary>
/// This denotes a person in a job of importance, whose spot will be reopened on cryo.
/// </summary>
[RegisterComponent, Access(typeof(JobTrackingSystem), typeof(GameTicker))]
public sealed partial class JobTrackingComponent : Component
{
    /// <summary>
    /// The station this entity spawned in on.
    /// If they enter cryo, a slot will be reopened if there are fewer open slots than there are empty default slots.
    /// </summary>
    [DataField]
    public EntityUid SpawnStation;

    /// <summary>
    /// The job this entity holds.  See above.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// The job this entity holds.  See above.
    /// </summary>
    [DataField]
    public bool Active;
}

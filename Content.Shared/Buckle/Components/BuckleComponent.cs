using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedBuckleSystem))]
public sealed partial class BuckleComponent : Component
{
    /// <summary>
    /// The range from which this entity can buckle to a <see cref="StrapComponent"/>.
    /// Separated from normal interaction range to fix the "someone buckled to a strap
    /// across a table two tiles away" problem.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = SharedInteractionSystem.InteractionRange / 1.4f;

    /// <summary>
    /// True if the entity is buckled, false otherwise.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Buckled;

    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? LastEntityBuckledTo;

    /// <summary>
    /// Whether or not collisions should be possible with the entity we are strapped to
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public bool DontCollide;

    /// <summary>
    /// Whether or not we should be allowed to pull the entity we are strapped to
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool PullStrap;

    /// <summary>
    /// The amount of time that must pass for this entity to
    /// be able to unbuckle after recently buckling.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// The time that this entity buckled at.
    /// </summary>
    [ViewVariables]
    public TimeSpan BuckleTime;

    /// <summary>
    /// The strap that this component is buckled to.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? BuckledTo;

    /// <summary>
    /// The amount of space that this entity occupies in a
    /// <see cref="StrapComponent"/>.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Size = 100;

    /// <summary>
    /// Used for client rendering
    /// </summary>
    [ViewVariables] public int? OriginalDrawDepth;
}

[ByRefEvent]
public record struct BuckleAttemptEvent(EntityUid StrapEntity, EntityUid BuckledEntity, EntityUid UserEntity, bool Buckling, bool Cancelled = false);

[ByRefEvent]
public readonly record struct BuckleChangeEvent(EntityUid StrapEntity, EntityUid BuckledEntity, bool Buckling);

[Serializable, NetSerializable]
public enum BuckleVisuals
{
    Buckled
}

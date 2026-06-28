// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.SpaceWhale;

/// <summary>
/// When given to an entity, creates X tailed entities that try to follow the entity with the component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TailedEntityComponent : Component
{
    /// <summary>
    /// amount of entities in between the tail and the head
    /// </summary>
    [DataField]
    public int Amount = 3;

    /// <summary>
    /// the entity/entities that should be spawned after the head
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<TailedEntitySegmentComponent> Prototype;

    /// <summary>
    /// How much space between entities
    /// </summary>
    [DataField]
    public float Spacing = 1f;

    /// <summary>
    /// Set to true if head should collide with segments
    /// This controls <see cref="PreventSegmentCollide"/>.
    /// To make head being able to collide/not collide with segments, use PreventCollide component or adjust Fixtures
    /// </summary>
    [DataField]
    public bool ShouldCollideWithSegments;

    /// <summary>
    /// If above 0, head won't collide with specified amount of segments, starting from the head
    /// Used to prevent contacting segments from pushing head
    /// </summary>
    [DataField]
    public int PreventFirstSegmentsCollideAmount;

    /// <summary>
    /// If true, melee attack range will be extended from head to segments
    /// </summary>
    [DataField]
    public bool MeleeAttackWithSegments = true;

    /// <summary>
    /// If true, head rotation will be automatically updated to match its segments
    /// Additionally, NoRotateOnMove will be added/removed from entity depending on segment count
    /// </summary>
    [DataField]
    public bool HeadFollowSegmentRotation = true;

    /// <summary>
    /// Used in entity lookup to check if head is touching any of its segments
    /// Uses lookup instead of physics checks because it also uses PreventCollideEvent
    /// </summary>
    [DataField]
    public float HeadRadius = 0.4f;

    /// <summary>
    /// If <see cref="ShouldCollideWithSegments"/> is true, this will prevent collision temporarily on map init and on forced contract
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool PreventSegmentCollide = true;

    /// <summary>
    /// List of tail segments
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<SegmentData> TailSegments = new();

    [DataField]
    public Vector2 LastPos;
}

[Serializable, NetSerializable, DataRecord]
public sealed partial class SegmentData(NetEntity segment, Vector2 worldPosition)
{
    public SegmentData() : this(NetEntity.Invalid, Vector2.Zero) { }

    public NetEntity Segment = segment;

    public Vector2 WorldPosition = worldPosition;
}

[Serializable, NetSerializable]
public enum TailedEntitySegmentLayer
{
    Base
}

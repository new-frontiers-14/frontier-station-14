// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.SpaceWhale;

/// <summary>
/// When given to an entity, creates X tailed entities that try to follow the entity with the component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    public EntProtoId Prototype;

    /// <summary>
    /// How much space between entities
    /// </summary>
    [DataField]
    public float Spacing = 1f;

    /// <summary>
    /// List of tail segments
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> TailSegments = new();

    [DataField]
    public Vector2 LastPos = Vector2.Zero;
}

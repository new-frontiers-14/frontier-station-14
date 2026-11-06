// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.

using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Shuttles.Events;

/// <summary>
/// Raised on the client when it wishes to track a particular coordinate.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetTargetCoordinatesRequest : BoundUserInterfaceMessage
{
    public NetEntity? ShuttleEntityUid { get; set; }
    public Vector2 TrackedPosition { get; set; }
    /// <summary>
    /// The entity that is being tracked.
    /// Currently only used for ID purposes, does not actually track the entity.
    /// </summary>
    public NetEntity TrackedEntity { get; set; }
}

/// <summary>
/// Raised on the client when it wishes to hide or show the target.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetHideTargetRequest : BoundUserInterfaceMessage
{
    public bool Hidden { get; set; }
}

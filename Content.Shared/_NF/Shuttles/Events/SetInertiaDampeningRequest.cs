// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Shuttles.Events
{
    /// <summary>
    /// Raised on the client when it wishes to change the inertial dampening of a ship.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class SetInertiaDampeningRequest : BoundUserInterfaceMessage
    {
        public NetEntity? ShuttleEntityUid { get; set; }
        public InertiaDampeningMode Mode { get; set; }
    }

    [Serializable, NetSerializable]
    public enum InertiaDampeningMode
    {
        Off = 0,
        Dampen = 1,
        Anchor = 2,
        Station = 3, // Reserved for station status, should not be used in requests.
    }
}

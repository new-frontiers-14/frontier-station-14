using Robust.Shared.Serialization;

namespace Content.Shared._WF.Shuttles.Events;

/// <summary>
/// Raised on the client when it wishes to toggle the autopilot of a ship.
/// </summary>
[Serializable, NetSerializable]
public sealed class ToggleAutopilotRequest : BoundUserInterfaceMessage
{
    public NetEntity? ShuttleEntityUid { get; set; }
}

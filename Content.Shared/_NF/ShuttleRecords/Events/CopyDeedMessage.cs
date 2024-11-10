using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords.Events;

/// <summary>
/// Message that is sent from the client to the server when the deed needs to be copied.
/// </summary>
[Serializable, NetSerializable]
public sealed class CopyDeedMessage(NetEntity shuttleNetEntity) : BoundUserInterfaceMessage
{
    public NetEntity ShuttleNetEntity { get; set; } = shuttleNetEntity;
}

using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Harmony.JoinQueue;

/// <summary>
///     Sent from server to client to update the queue information.
/// </summary>
public sealed class MsgQueueUpdate : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableSequenced; // Make sure the message gets in, but if it's late we don't care about it.

    /// <summary>
    ///     Total players in queue
    /// </summary>
    public int Total;

    /// <summary>
    ///     Player current position in queue (starts from 1)
    /// </summary>
    public int Position;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Total = buffer.ReadInt32();
        Position = buffer.ReadInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Total);
        buffer.Write(Position);
    }
}

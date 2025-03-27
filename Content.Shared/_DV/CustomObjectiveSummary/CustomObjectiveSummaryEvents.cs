using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.CustomObjectiveSummary;

/// <summary>
///     Message from the client with what they are updating their summary to.
/// </summary>
public sealed class CustomObjectiveClientSetObjective : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    /// <summary>
    ///     The summary that the user wrote.
    /// </summary>
    public string Summary = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Summary = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Summary);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
}

/// <summary>
///     Clients listen for this event and when they get it, they open a popup so the player can fill out the objective summary.
/// </summary>
[Serializable, NetSerializable]
public sealed class CustomObjectiveSummaryOpenMessage : EntityEventArgs;

/// <summary>
///     DeltaV event for when the evac shuttle leaves.
/// </summary>
[Serializable, NetSerializable]
public sealed class EvacShuttleLeftEvent : EventArgs;

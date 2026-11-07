using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._NF.River.Events;

/// <summary>
/// Event sent from the server to the client containing all the river nodes.
/// </summary>
[Serializable, NetSerializable]
public sealed class TransferRiverNodesEvent : EntityEventArgs
{
    public readonly List<NetEntity> NodeList;

    public TransferRiverNodesEvent(List<NetEntity> nodeList)
    {
        NodeList = nodeList;
    }
}

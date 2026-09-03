//using Content.Client._NF.River.Components;
using Content.Shared._NF.River.Components;
using Content.Shared._NF.River.Events;
using Content.Shared._NF.River;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._NF.River;

public sealed partial class RiverNodeSystem : SharedRiverNodeSystem
{
    private List<EntityUid> _nodeList = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TransferRiverNodesEvent>(TransferNodes);
    }
    private void TransferNodes(TransferRiverNodesEvent transferEvent)
    {
        if (transferEvent?.NodeList == null)
        {
            _nodeList.Clear();
            return;
        }
        _nodeList = GetEntityList(transferEvent.NodeList);
    }
    public List<EntityUid> GetNodeList()
    {
        return _nodeList;
    }
    //public List<EntityUid> CreateNodeList()
    //{
    //    List<EntityUid> nodeList = new();
    //    var riverNodes = EntityQueryEnumerator<RiverNodeComponent, TransformComponent>();
    //    while (riverNodes.MoveNext(out var uid, out var riverComp, out var transComp))
    //    {
    //        nodeList.Add(uid);
    //    }
    //    return nodeList;
    //}
    public bool TryGetNodeData(EntityUid node, [NotNullWhen(true)] out RiverNodeComponent? nodeComp)
    {
        var returnValue = TryComp<RiverNodeComponent>(node, out var tempComp);
        nodeComp = tempComp;
        return returnValue;
    }
}

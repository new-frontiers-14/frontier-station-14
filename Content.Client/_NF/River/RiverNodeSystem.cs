//using Content.Client._NF.River.Components;
using Content.Shared._NF.River;
using Content.Shared._NF.River.Components;
using Content.Shared._NF.River.Events;
using Robust.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Content.Shared._NF.River.Components.RiverNodeComponent;

namespace Content.Client._NF.River;

public sealed partial class RiverNodeSystem : SharedRiverNodeSystem
{
    public readonly struct NodeLink(
        EntityUid node,
        Vector2 controlPoint)
    {
        public EntityUid Node { get; } = node;
        public Vector2 ControlPoint { get; } = controlPoint;
    }
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

    public List<NodeLink> GetNextNodes(RiverNodeComponent node)
    {
        List<NodeLink> returnList = new();
        foreach (NetNodeLink netLink in node.NextNodes)
        {
            var link = new NodeLink(GetEntity(netLink.Node), netLink.ControlPoint);
            returnList.Add(link);
        }
        return returnList;
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

    public List<Vector2> CalcCurveSections(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, int totalLines)
    {
        List<Vector2> result = new();
        float increment = 1f / totalLines;
        for (var i = 0; i <= totalLines; i++)
        {
            var section = i * increment;
            var newPoint = MathF.Pow((1 - section), 2) * startPoint + 2 * (1 - section) * section * controlPoint + MathF.Pow(section, 2) * endPoint;
            result.Add(newPoint);
        }
        return result;
    }
}

using Content.Server._NF.River.Components;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using System.Data;
using System.Numerics;

namespace Content.Server._NF.River.Systems;

public partial class RiverNodeSystem
{
    private struct Connection
    {
        public Connection(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        public override string ToString() => $"({Start}, {End})";
    }
    public void POIArterialRiverGen(MapId mapId, List<EntityUid> pois)
    {
        // There's a *lot* of loops in this, this can probably be optimised, but it's only run once, so this will do for now.
        var unitDistance = 2500f; //2500 or 3000? TODO: make this not a magic number.
        var validPois = FilterValidPOIs(mapId, pois);
        var poiPositions = GetPOIPositions(validPois);
        poiPositions.Add(new Vector2(0f, 0f)); // Adds Frontier Station.

        // Connect all the POIs that are in range of eachother
        List<Connection> connections = new();

        List<Vector2> handledPositions = new();
        List<Vector2> unconnectedPositions = new();
        foreach (var poiPos in poiPositions)
        {
            handledPositions.Add(poiPos);
            var connected = false;
            foreach (var otherPoiPos in poiPositions)
            {
                if (handledPositions.Contains(otherPoiPos))
                {
                    continue;
                }
                if ((poiPos - otherPoiPos).Length() < unitDistance)
                {
                    connections.Add(new Connection(poiPos, otherPoiPos));
                    connected = true;
                }
            }
            if (!connected)
            {
                unconnectedPositions.Add(poiPos);
            }
        }

        // Cull all lines that overlap
        List<Connection> validConnections = new();
        validConnections.AddRange(connections);

        List<Connection> handledConnections = new();
        foreach (var connection in connections)
        {
            handledConnections.Add(connection);
            foreach (var otherConnection in connections)
            {
                if (handledConnections.Contains(otherConnection))
                {
                    continue;
                }
                if (CheckIntersect(connection, otherConnection))
                {
                    validConnections.Remove(connection);
                    validConnections.Remove(otherConnection);
                }
            }
        }

        // Check to make sure there is one, completely connected network, not multiple separate ones (and every valid POI is connected)
        List<List<Vector2>> networks = new();
        foreach (var position in unconnectedPositions)
        {
            List<Vector2> tinyNetwork = new();
            tinyNetwork.Add(position);
            networks.Add(tinyNetwork);
        }
        handledPositions.Clear();
        handledPositions.AddRange(unconnectedPositions);

        var safetyLimit = 100;
        foreach (var position in poiPositions)
        {
            if (handledPositions.Contains(position))
            {
                continue;
            }
            handledPositions.Add(position);
            List<Vector2> posInNetwork = new();
            posInNetwork.Add(position);
            var networkFilled = false;
            while (!networkFilled && safetyLimit > 0)
            {
                var somethingAdded = false;
                foreach (var connection in validConnections)
                {
                    if (posInNetwork.Contains(connection.Start) && !posInNetwork.Contains(connection.End))
                    {
                        posInNetwork.Add(connection.End);
                        if (!handledPositions.Contains(connection.End))
                        {
                            handledPositions.Add(connection.End);
                        }
                        somethingAdded = true;
                    }
                    else if (posInNetwork.Contains(connection.End) && !posInNetwork.Contains(connection.Start))
                    {
                        posInNetwork.Add(connection.Start);
                        if (!handledPositions.Contains(connection.Start))
                        {
                            handledPositions.Add(connection.Start);
                        }
                        somethingAdded = true;
                    }
                }
                if (!somethingAdded)
                {
                    networkFilled = true;
                }
                safetyLimit--;
            }
            networks.Add(posInNetwork);
        }

        safetyLimit = 100;
        while (networks.Count > 1 && safetyLimit > 0)
        {
            // More than one network! Connect them to eachother!
            var focusNetwork = networks[0];
            List<Vector2> averagePositions = new();
            foreach (var network in networks)
            {
                Vector2 averagePos = new();
                foreach (var position in network)
                {
                    averagePos = averagePos + position;
                }
                averagePos = averagePos / network.Count;
                averagePositions.Add(averagePos);
            }

            var closestNetworkIndex = -1;
            var closestNetworkDistance = -1f;

            for (var i = 1; i < networks.Count; i++)
            {
                var distance = (averagePositions[i] - averagePositions[0]).Length();
                if (closestNetworkDistance == -1f || closestNetworkDistance > distance)
                {
                    closestNetworkDistance = distance;
                    closestNetworkIndex = i;
                }
            }
            if (closestNetworkIndex == -1)
            {
                break; //Something went wrong!
            }
            var closestNetwork = networks[closestNetworkIndex];
            Vector2 closestFocusPos = new();
            Vector2 closestOtherPos = new();
            var closestDistance = -1f;
            for (var i = 0; i < focusNetwork.Count; i++)
            {
                for (var j = 0; j < closestNetwork.Count; j++)
                {
                    if (closestDistance == -1)
                    {
                        closestFocusPos = focusNetwork[i];
                        closestOtherPos = closestNetwork[j];
                        closestDistance = (closestOtherPos - closestFocusPos).Length();
                    }
                    else
                    {
                        var distance = (closestNetwork[j] - focusNetwork[i]).Length();
                        if (distance < closestDistance)
                        {
                            closestFocusPos = focusNetwork[i];
                            closestOtherPos = closestNetwork[j];
                            closestDistance = distance;
                        }
                    }
                }
            }
            // Make a connection, and merge the two networks in the "networks" list.
            validConnections.Add(new Connection(closestFocusPos, closestOtherPos));
            List<Vector2> tempNetwork = new();
            tempNetwork.AddRange(closestNetwork);
            networks[0].AddRange(closestNetwork);
            networks.Remove(tempNetwork);

            safetyLimit--;
        }

        foreach (var connection in validConnections)
        {
            CreateRiver(mapId, connection.Start, connection.End);
        }
    }

    public bool CreateRiver(MapId mapId, Vector2 startLocation, Vector2 endLocation)
    {
        //TODO: Make this something that's not horrible. *Very* placeholder, just draws long as lines for now.

        var startCoord = new MapCoordinates(startLocation, mapId);
        var endCoord = new MapCoordinates(endLocation, mapId);
        var startNode = Spawn("SpaceRiverNode", startCoord);
        var endNode = Spawn("SpaceRiverNode", endCoord);

        var flowDirection = Vector2.Normalize(endLocation - startLocation).ToAngle();
        if (!TryComp<RiverNodeComponent>(startNode, out var startComp)
            || !TryComp<RiverNodeComponent>(endNode, out var endComp))
        {
            return false;
        }

        startComp.FlowDirection = flowDirection;
        endComp.FlowDirection = flowDirection;

        startComp.ForwardControlPoint = startComp.FlowDirection.ToVec() * startComp.Boost * 50f;
        endComp.ForwardControlPoint = endComp.FlowDirection.ToVec() * endComp.Boost * 50f;

        startComp.NextNodes.Add(GetNetEntity(endNode));
        endComp.PriorNodes.Add(GetNetEntity(startNode));
        Dirty(startNode, startComp);
        Dirty(endNode, endComp);

        return true;
    }

    /// <summary>
    /// Filters out any POI with a [Hide] flag on its IFFComponent or on a different map. This is so that the Cove and Listening Point Bravo cannot be found through meta-knowledge
    /// of river generation, as well as automatically making future POIs that probably don't want to be found, not connect automatically to rivers.
    /// Hidden POIs with the ForceRiverConnectionComponent will still be added.
    /// </summary>
    /// <param name="mapId">The ID of the map on which the rivers should be generated</param>
    /// <param name="pois">A list of all POIs</param>
    /// <returns>A list of all POIs that don't have the [Hide] IFFFlag.</returns>
    private List<EntityUid> FilterValidPOIs(MapId mapId, List<EntityUid> pois)
    {
        List<EntityUid> validPois = new();
        foreach (var poi in pois)
        {
            if (!TryComp<IFFComponent>(poi, out var poiIFFComp))
            {
                continue;
            }
            var iffFlag = poiIFFComp.Flags;
            if ((!iffFlag.HasFlag(IFFFlags.Hide) || TryComp<ForceRiverConnectionComponent>(poi, out var _)) && _transform.GetMapId(poi) == mapId)
            {
                validPois.Add(poi);
            }
        }
        return validPois;
    }

    /// <summary>
    /// Gets the world positions of each POI(entity) in a list.
    /// </summary>
    /// <param name="pois">The list of POI EntityUids whose positions are to be retrieved.</param>
    /// <returns>A list of WorldPosition vectors of the inserted POIs</returns>
    private List<Vector2> GetPOIPositions(List<EntityUid> pois)
    {
        List<Vector2> poiCoordinates = new();
        foreach (var poi in pois)
        {
            poiCoordinates.Add(_transform.GetWorldPosition(poi));
        }
        return poiCoordinates;
    }

    /// <summary>
    /// Checks if two Connections (Segments) intersect anywhere other than at their vertex points.
    /// </summary>
    /// <param name="line1">First line</param>
    /// <param name="line2">Second line</param>
    /// <returns>True if the Connections intersect anywhere other than their vertex points, else returns false.</returns>
    private bool CheckIntersect(Connection line1, Connection line2)
    {
        var dX1 = line1.End.X - line1.Start.X;
        var dX2 = line2.End.X - line2.Start.X;
        var dY1 = line1.End.Y - line1.Start.Y;
        var dY2 = line2.End.Y - line2.Start.Y;

        var p0 = dY2 * (line2.End.X - line1.Start.X) - dX2 * (line2.End.Y - line1.Start.Y);
        var p1 = dY2 * (line2.End.X - line1.End.X) - dX2 * (line2.End.Y - line1.End.Y);
        var p2 = dY1 * (line1.End.X - line2.Start.X) - dX1 * (line1.End.Y - line2.Start.Y);
        var p3 = dY1 * (line1.End.X - line2.End.X) - dX1 * (line1.End.Y - line2.End.Y);

        return (p0 * p1 < 0 && p2 * p3 < 0);
    }
}

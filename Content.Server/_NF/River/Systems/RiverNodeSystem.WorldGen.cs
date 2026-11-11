using Content.Server._NF.River.Components;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server._NF.River.Systems;

public partial class RiverNodeSystem
{
    private struct Connection
    {
        public Connection(EntityUid start, EntityUid end)
        {
            Start = start;
            End = end;
        }
        public EntityUid Start { get; set; }
        public EntityUid End { get; set; }

        public override string ToString() => $"({Start}, {End})";
    }
    public void POIArterialRiverGen(MapId mapId, List<EntityUid> pois)
    {
        // There's a *lot* of loops in this, this can probably be optimised, but it's only run once, so this will do for now.
        var unitDistance = 3000f; //2500 or 3000? TODO: make this not a magic number.
        var validPois = FilterValidPOIs(mapId, pois);
        var poiPositions = GetPOIPositions(validPois);
        poiPositions.Add(new Vector2(0f, 0f)); // Adds Frontier Station
        List<EntityUid> wells = new();

        foreach (var poi in poiPositions)
        {
            var random = _random.Next(2);
            var spin = Chirality.Counterclockwise;
            if (random == 0)
            {
                spin = Chirality.Clockwise;
            }
            wells.Add(CreateWell(mapId, poi, 200f, spin));
        }

        // Connect all the POIs that are in range of eachother
        List<Connection> connections = new();

        List<EntityUid> handledWells = new();
        List<EntityUid> unconnectedWells = new();
        foreach (var well in wells)
        {
            handledWells.Add(well);
            var connected = false;
            foreach (var otherWell in wells)
            {
                if (handledWells.Contains(otherWell))
                {
                    continue;
                }
                var wellPosition = _transform.GetWorldPosition(well);
                var otherWellPosition = _transform.GetWorldPosition(otherWell);
                if ((wellPosition - otherWellPosition).Length() < unitDistance)
                {
                    var tooCloseToPoi = false;
                    foreach (var distanceCheckWell in wells)
                    {
                        if (distanceCheckWell == well || distanceCheckWell == otherWell)
                        {
                            continue;
                        }
                        var distanceCheckWellPos = _transform.GetWorldPosition(distanceCheckWell);
                        if (DistanceToLineSegmentSquared(distanceCheckWellPos, wellPosition, otherWellPosition, out var _, out var t) < 300f || t > 1f || t < 0f) //300f is pretty arbitrary, but 200 radius + 100 buffer.
                        {
                            tooCloseToPoi = true;
                            break;
                        }
                    }
                    if (!tooCloseToPoi)
                    {
                        connections.Add(new Connection(well, otherWell));
                        connected = true;
                    }
                }
            }
            if (!connected && !ContainsWell(well, connections))
            {
                unconnectedWells.Add(well);
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
        List<List<EntityUid>> networks = new();
        foreach (var well in unconnectedWells)
        {
            List<EntityUid> tinyNetwork = new();
            tinyNetwork.Add(well);
            networks.Add(tinyNetwork);
        }
        handledWells.Clear();
        handledWells.AddRange(unconnectedWells);

        var safetyLimit = 100;
        foreach (var well in wells)
        {
            if (handledWells.Contains(well))
            {
                continue;
            }
            handledWells.Add(well);
            List<EntityUid> wellInNetwork = new();
            wellInNetwork.Add(well);
            var networkFilled = false;
            while (!networkFilled && safetyLimit > 0)
            {
                var somethingAdded = false;
                foreach (var connection in validConnections)
                {
                    if (wellInNetwork.Contains(connection.Start) && !wellInNetwork.Contains(connection.End))
                    {
                        wellInNetwork.Add(connection.End);
                        if (!handledWells.Contains(connection.End))
                        {
                            handledWells.Add(connection.End);
                        }
                        somethingAdded = true;
                    }
                    else if (wellInNetwork.Contains(connection.End) && !wellInNetwork.Contains(connection.Start))
                    {
                        wellInNetwork.Add(connection.Start);
                        if (!handledWells.Contains(connection.Start))
                        {
                            handledWells.Add(connection.Start);
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
            networks.Add(wellInNetwork);
        }

        //Handle and consolidate separate networks.
        safetyLimit = 100;
        while (networks.Count > 1 && safetyLimit > 0)
        {
            // focusNetwork should be (one of) the largest one(s) to make connections more convenient.
            var focusNetwork = new List<EntityUid>();
            var largestNetworkIndex = 0;
            var largestNetworkCount = 0;
            var iterator = 0;
            List<Vector2> averagePositions = new();
            foreach (var network in networks)
            {
                if (network.Count > largestNetworkCount)
                {
                    largestNetworkIndex = iterator;
                    largestNetworkCount = network.Count;
                }
                Vector2 averagePos = new();
                foreach (var well in network)
                {
                    averagePos = averagePos + _transform.GetWorldPosition(well);
                }
                averagePos = averagePos / network.Count;
                averagePositions.Add(averagePos);
                iterator++;
            }

            focusNetwork = networks[largestNetworkIndex];

            var closestNetworkIndex = -1;
            var closestNetworkDistance = -1f;

            for (var i = 0; i < networks.Count; i++)
            {
                if (largestNetworkIndex == i)
                {
                    continue;
                }
                var distance = (averagePositions[i] - averagePositions[largestNetworkIndex]).Length();
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
            EntityUid closestFocusWell = new();
            EntityUid closestOtherWell = new();
            var closestDistance = -1f;
            for (var i = 0; i < focusNetwork.Count; i++)
            {
                for (var j = 0; j < closestNetwork.Count; j++)
                {
                    if (closestDistance == -1)
                    {
                        closestFocusWell = focusNetwork[i];
                        closestOtherWell = closestNetwork[j];
                        var focusWellPos = _transform.GetWorldPosition(closestFocusWell);
                        var otherWellPos = _transform.GetWorldPosition(closestOtherWell);
                        closestDistance = (otherWellPos - focusWellPos).Length();
                    }
                    else
                    {
                        var focusPos = _transform.GetWorldPosition(focusNetwork[i]);
                        var closestPos = _transform.GetWorldPosition(closestNetwork[j]);
                        var distance = (closestPos - focusPos).Length();
                        if (distance < closestDistance)
                        {
                            closestFocusWell = focusNetwork[i];
                            closestOtherWell = closestNetwork[j];
                            closestDistance = distance;
                        }
                    }
                }
            }
            // Make a connection, and merge the two networks in the "networks" list.
            validConnections.Add(new Connection(closestFocusWell, closestOtherWell));
            networks[largestNetworkIndex].AddRange(closestNetwork);
            networks.Remove(closestNetwork);

            safetyLimit--;
        }

        // Inform the wells of which wells they're connected to.
        foreach (var connection in validConnections)
        {
            if (!TryComp<RiverWellComponent>(connection.Start, out var startWellComp) ||
                !TryComp<RiverWellComponent>(connection.End, out var endWellComp))
            {
                continue;
            }

            if (!startWellComp.ConnectedWells.Contains(connection.End))
            {
                startWellComp.ConnectedWells.Add(connection.End);
            }
            if (!endWellComp.ConnectedWells.Contains(connection.Start))
            {
                endWellComp.ConnectedWells.Add(connection.Start);
            }
        }

        OrganiseWells(wells);

        // Create the actual rivers.
        foreach (var connection in validConnections)
        {
            CreateConnectingRiverPair(mapId, connection.Start, connection.End);
        }
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
        var line1Start = _transform.GetWorldPosition(line1.Start);
        var line1End = _transform.GetWorldPosition(line1.End);
        var line2Start = _transform.GetWorldPosition(line2.Start);
        var line2End = _transform.GetWorldPosition(line2.End);

        var dX1 = line1End.X - line1Start.X;
        var dX2 = line2End.X - line2Start.X;
        var dY1 = line1End.Y - line1Start.Y;
        var dY2 = line2End.Y - line2Start.Y;

        var p0 = dY2 * (line2End.X - line1Start.X) - dX2 * (line2End.Y - line1Start.Y);
        var p1 = dY2 * (line2End.X - line1End.X) - dX2 * (line2End.Y - line1End.Y);
        var p2 = dY1 * (line1End.X - line2Start.X) - dX1 * (line1End.Y - line2Start.Y);
        var p3 = dY1 * (line1End.X - line2End.X) - dX1 * (line1End.Y - line2End.Y);

        return (p0 * p1 < 0 && p2 * p3 < 0);
    }

    /// <summary>
    /// Checks if a position is within a Connections list.
    /// </summary>
    /// <param name="position">The position you wish to check is in the list</param>
    /// <param name="connections">The list you wish to check</param>
    /// <returns>Whether the position is in the list (true) or not (false)</returns>
    private bool ContainsWell(EntityUid well, List<Connection> connections)
    {
        foreach (var connection in connections)
        {
            if (connection.Start == well || connection.End == well)
            {
                return true;
            }
        }

        return false;
    }
}

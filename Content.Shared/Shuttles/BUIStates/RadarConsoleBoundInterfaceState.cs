using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
[Virtual]
public class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public readonly float MaxRange;

    /// <summary>
    /// The relevant coordinates to base the radar around.
    /// </summary>
    public NetCoordinates? Coordinates;

    /// <summary>
    /// The relevant rotation to rotate the angle around.
    /// </summary>
    public Angle? Angle;

    public readonly List<DockingInterfaceState> Docks;

    public RadarConsoleBoundInterfaceState(
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks)
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
    }
}

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class DockingInterfaceState
{
    public NetCoordinates Coordinates;
    public Angle Angle;
    public NetEntity Entity;
    public bool Connected;
    public Color Color;
    public string? Name;
    public Color HighlightedColor;
}

public class DockComparer: IComparer<DockingInterfaceState>
{
    public int Compare(DockingInterfaceState? a, DockingInterfaceState? b)
    {
        if (a == null)
            return -1;
        else if (b == null)
            return 1;
        else if (a.Name != null && b.Name != null)
            return a.Name.CompareTo(b.Name);
        else if (a.Name != null) // a has a name but b doesn't
            return 1;
        else // neither are named, compare by Uid
            return a.Entity.CompareTo(b.Entity);
    }
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}

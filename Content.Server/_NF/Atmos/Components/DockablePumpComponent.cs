namespace Content.Server._NF.Atmos.Components;

public sealed partial class DockablePumpComponent : Component
{
    /// <summary>
    /// The name of the node that is available to dock.
    /// </summary>
    [DataField]
    public string DockNodeName;

    /// <summary>
    /// The name of the internal node
    /// </summary>
    [DataField]
    public string InternalNodeName;

    /// <summary>
    /// If true, the pump will be pumping inwards (dock node to internal node).
    /// </summary>
    [DataField]
    public bool PumpingInwards;
}

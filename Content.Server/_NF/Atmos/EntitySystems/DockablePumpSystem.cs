using Content.Server._NF.Atmos.Components;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Shuttles.Events;
using Robust.Shared.Timing;

namespace Content.Server._NF.Atmos.EntitySystems;

public sealed partial class DockablePumpSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DockablePumpComponent, DockEvent>(OnDock);
        SubscribeLocalEvent<DockablePumpComponent, UndockEvent>(OnUndock);
    }

    public void SetPipeDirection(EntityUid uid, DockablePumpComponent component, bool inwards)
    {
        if (component.PumpingInwards == inwards)
            return;

        if (TryComp(uid, out GasPressurePumpComponent? pressurePump))
        {
            pressurePump.InletName = inwards ? component.DockNodeName : component.InternalNodeName;
            pressurePump.OutletName = inwards ? component.InternalNodeName : component.DockNodeName;
        }
        else if (TryComp(uid, out GasVolumePumpComponent? volumePump))
        {
            volumePump.InletName = inwards ? component.DockNodeName : component.InternalNodeName;
            volumePump.OutletName = inwards ? component.InternalNodeName : component.DockNodeName;
        }
        else return; // Not a pump type we support.

        component.PumpingInwards = inwards;
    }

    private void OnDock(EntityUid uid, DockablePumpComponent component, ref DockEvent args)
    {
        // Reflood node?
        if (string.IsNullOrEmpty(component.DockNodeName) ||
            !TryComp(uid, out NodeContainerComponent? nodeContainer) ||
            !_nodeContainer.TryGetNode(nodeContainer, component.DockNodeName, out DockablePipeNode? dockablePipe))
            return;

        _nodeGroup.QueueReflood(dockablePipe);
    }

    private void OnUndock(EntityUid uid, DockablePumpComponent component, ref UndockEvent args)
    {
        // Clean up node?
        if (string.IsNullOrEmpty(component.DockNodeName) ||
            !TryComp(uid, out NodeContainerComponent? nodeContainer) ||
            !_nodeContainer.TryGetNode(nodeContainer, component.DockNodeName, out DockablePipeNode? dockablePipe))
            return;

        _nodeGroup.QueueNodeRemove(dockablePipe);
    }
}

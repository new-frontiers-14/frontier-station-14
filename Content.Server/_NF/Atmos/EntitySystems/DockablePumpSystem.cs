using Content.Server._NF.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Shuttles.Events;
using Content.Shared.Atmos.Visuals;
using Robust.Server.GameObjects;

namespace Content.Server._NF.Atmos.EntitySystems;

public sealed partial class DockablePumpSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DockablePumpComponent, DockEvent>(OnDock);
        SubscribeLocalEvent<DockablePumpComponent, UndockEvent>(OnUndock);
    }

    private void OnDock(EntityUid uid, DockablePumpComponent component, ref DockEvent args)
    {
        // Reflood node?
        if (string.IsNullOrEmpty(component.DockNodeName) ||
            !TryComp(uid, out NodeContainerComponent? nodeContainer) ||
            !_nodeContainer.TryGetNode(nodeContainer, component.DockNodeName, out DockablePipeNode? dockablePipe))
            return;

        _nodeGroup.QueueReflood(dockablePipe);
        _appearance.SetData(uid, DockablePumpVisuals.Docked, true);
    }

    private void OnUndock(EntityUid uid, DockablePumpComponent component, ref UndockEvent args)
    {
        // Clean up node?
        if (string.IsNullOrEmpty(component.DockNodeName) ||
            !TryComp(uid, out NodeContainerComponent? nodeContainer) ||
            !_nodeContainer.TryGetNode(nodeContainer, component.DockNodeName, out DockablePipeNode? dockablePipe))
            return;

        _nodeGroup.QueueNodeRemove(dockablePipe);
        dockablePipe.Air.Clear();
        _appearance.SetData(uid, DockablePumpVisuals.Docked, false);
    }
}

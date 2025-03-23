using Content.Server._NF.Atmos.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Shuttles.Events;
using Content.Shared._NF.Atmos.Visuals;
using Robust.Server.GameObjects;

namespace Content.Server._NF.Atmos.Systems;

public sealed class DockablePipeSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DockablePipeComponent, DockEvent>(OnDock);
        SubscribeLocalEvent<DockablePipeComponent, UndockEvent>(OnUndock);
    }

    private void OnDock(Entity<DockablePipeComponent> ent, ref DockEvent args)
    {
        // Reflood node?
        if (string.IsNullOrEmpty(ent.Comp.DockNodeName) ||
            !TryComp(ent, out NodeContainerComponent? nodeContainer) ||
            !_nodeContainer.TryGetNode(nodeContainer, ent.Comp.DockNodeName, out DockablePipeNode? dockablePipe))
            return;

        _nodeGroup.QueueReflood(dockablePipe);
        _appearance.SetData(ent, DockablePipeVisuals.Docked, true);
    }

    private void OnUndock(Entity<DockablePipeComponent> ent, ref UndockEvent args)
    {
        // Clean up node?
        if (string.IsNullOrEmpty(ent.Comp.DockNodeName) ||
            !TryComp(ent, out NodeContainerComponent? nodeContainer) ||
            !_nodeContainer.TryGetNode(nodeContainer, ent.Comp.DockNodeName, out DockablePipeNode? dockablePipe))
            return;

        _nodeGroup.QueueNodeRemove(dockablePipe);
        dockablePipe.Air.Clear();
        _appearance.SetData(ent, DockablePipeVisuals.Docked, false);
    }
}

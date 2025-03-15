using Content.Shared.Rounding;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Storage.EntitySystems;

public sealed class ContainerCountVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContainerCountVisualizerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ContainerCountVisualizerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ContainerCountVisualizerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnStartup(EntityUid uid, ContainerCountVisualizerComponent component, ComponentStartup args)
    {
        UpdateAppearance(uid, component: component);
    }

    private void OnInserted(EntityUid uid, ContainerCountVisualizerComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance(uid, component: component);
    }

    private void OnRemoved(EntityUid uid, ContainerCountVisualizerComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateAppearance(uid, component: component);
    }

    private void UpdateAppearance(EntityUid uid, AppearanceComponent? appearance = null,
        ContainerCountVisualizerComponent? component = null)
    {
        if (!Resolve(uid, ref appearance, ref component, false))
            return;

        if (component.MaxFillLevels < 1)
            return;

        if (!_container.TryGetContainer(uid, component.ContainerName, out var container))
            return;

        var level = ContentHelpers.RoundToLevels(container.Count, component.MaxCount, component.MaxFillLevels);
        _appearance.SetData(uid, StorageFillVisuals.FillLevel, level, appearance);
    }
}

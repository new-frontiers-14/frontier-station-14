
using Content.Shared._NF.Cargo.Components;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Shared._NF.Cargo.EntitySystems;

/// <summary>
/// Functions related to crate storage racks.
/// </summary>
public sealed class CrateStorageRackSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrateStorageRackComponent, ExaminedEvent>(OnRackExamined);
    }

    private void OnRackExamined(Entity<CrateStorageRackComponent> ent, ref ExaminedEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerName, out var rackContainer))
            return;

        args.PushMarkup(Loc.GetString("crate-storage-rack-examine", ("count", rackContainer.Count)));

        foreach (var item in rackContainer.ContainedEntities)
        {
            if (!TryComp(item, out MetaDataComponent? metadata))
                continue;

            args.PushMarkup(metadata.EntityName);
        }
    }
}

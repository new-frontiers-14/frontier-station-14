// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC.HTN;
using Content.Shared._Goobstation.Containers.ExtendedContainer;
using Content.Shared.NPC;
using Robust.Shared.Containers;

namespace Content.Server._Goobstation.Containers.ExtendedContainer;

public sealed class ExtendedContainerAISystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtendedContainerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ExtendedContainerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntInserted(EntityUid uid, ExtendedContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.ContainerName)
            return;

        var contained = EnsureComp<DropPodContainedComponent>(args.Entity);

        if (TryComp<HTNComponent>(args.Entity, out var htn))
        {
            contained.HadHtn = true;
            contained.HtnWasEnabled = htn.Enabled;
            htn.Enabled = false;
        }

        contained.HadActiveNpc = HasComp<ActiveNPCComponent>(args.Entity);
        RemCompDeferred<ActiveNPCComponent>(args.Entity);
    }

    private void OnEntRemoved(EntityUid uid, ExtendedContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.ContainerName)
            return;

        if (!TryComp<DropPodContainedComponent>(args.Entity, out var contained))
            return;

        if (contained.HadHtn && TryComp<HTNComponent>(args.Entity, out var htn))
            htn.Enabled = contained.HtnWasEnabled;

        if (contained.HadActiveNpc)
            EnsureComp<ActiveNPCComponent>(args.Entity);

        RemCompDeferred<DropPodContainedComponent>(args.Entity);
    }
}

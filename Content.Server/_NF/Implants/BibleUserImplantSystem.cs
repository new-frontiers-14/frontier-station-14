using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Server.Bible.Components;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class BibleUserImplantSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BibleUserImplantComponent, ImplantImplantedEvent>(OnInsert);
        // We need to remove the BibleUserComponent from the owner before the implant
        // is removed, so we need to execute before the SubdermalImplantSystem.
        SubscribeLocalEvent<BibleUserImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove, before: new[] { typeof(SubdermalImplantSystem) });
    }

    private void OnInsert(EntityUid uid, BibleUserImplantComponent component, ImplantImplantedEvent args)
    {
        if (!args.Implanted.HasValue)
            return;

        var bibleUserComp = EnsureComp<BibleUserComponent>(args.Implanted.Value);
        Dirty(args.Implanted.Value, bibleUserComp);
    }

    // Currently permanent, but should support removal if/when a viable solution is found.
    private void OnRemove(EntityUid uid, BibleUserImplantComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<SubdermalImplantComponent>(uid, out var implanted) || implanted.ImplantedEntity == null)
            return;

        RemComp<BibleUserComponent>(implanted.ImplantedEntity.Value);
    }
}

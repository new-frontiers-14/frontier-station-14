using Content.Server.Cargo.Components;
using Content.Shared.Mind;
using Content.Shared.Species.Components;
using static Content.Shared.Species.ReformSystem;

namespace Content.Server._NF.Species.Systems;

// Frontier - This adds cargo sell blacklist component to the newly reformed diona.
public sealed partial class ReformSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SetDionaCargoBlacklistEvent>(OnDionaReformed);
    }

    private void OnDionaReformed(SetDionaCargoBlacklistEvent ev)
    {
        EnsureComp<CargoSellBlacklistComponent>(ev.ReformedDiona);
    }
}

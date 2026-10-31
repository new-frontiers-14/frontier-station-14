using Content.Shared.Buckle;
using Content.Shared._NF.Roles.Components; // Frontier

namespace Content.Server.Traits.Assorted;

public sealed partial class BuckleOnMapInitSystem : EntitySystem
{
    [Dependency] private SharedBuckleSystem _buckleSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!; // Goobstation

    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleOnMapInitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BuckleOnMapInitComponent component, MapInitEvent args)
    {
        if (HasComp<InterviewHologramComponent>(uid)) // Frontier: FIXME - hacky bugfix for interview holograms
            return; // Frontier

        var buckle = Spawn(component.Prototype, _transform.GetMapCoordinates(uid)); // Goob edit: Transform.Coordinates<_transform.GetMapCoordinates
        _buckleSystem.TryBuckle(uid, uid, buckle);
    }
}

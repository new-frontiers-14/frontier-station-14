using System.Numerics;
using Content.Server._DV.Weapons.Ranged.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._DV.Weapons.Ranged.Systems;

public sealed class FireOnLandSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FireOnLandComponent, LandEvent>(FireOnLand);
    }

    private void FireOnLand(Entity<FireOnLandComponent> ent, ref LandEvent args)
    {
        if (!_random.Prob(ent.Comp.Probability) || !TryComp(ent, out GunComponent? gc))
            return;

        var dir = gc.DefaultDirection;
        dir = new Vector2(-dir.Y, dir.X); // 90 degrees counter-clockwise, guns shoot down by default
        _gunSystem.AttemptShoot(ent, ent, gc, new EntityCoordinates(ent, dir));
    }
}

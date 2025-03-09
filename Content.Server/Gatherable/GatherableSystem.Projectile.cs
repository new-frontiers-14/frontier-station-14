using Content.Server.Gatherable.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem
{
    private void InitializeProjectile()
    {
        SubscribeLocalEvent<GatheringProjectileComponent, StartCollideEvent>(OnProjectileCollide);
    }

    private void OnProjectileCollide(Entity<GatheringProjectileComponent> gathering, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            gathering.Comp.Amount <= 0 ||
            !TryComp<GatherableComponent>(args.OtherEntity, out var gatherable))
        {
            return;
        }

        // Frontier: gathering changes
        // bad gatherer - not strong enough
        if (_whitelistSystem.IsWhitelistFail(gatherable.ToolWhitelist, gathering.Owner))
        {
            QueueDel(gathering);
            return;
        }
        // Too strong (e.g. overpen) - gathers ore but destroys it
        if (TryComp<OreVeinComponent>(args.OtherEntity, out var oreVein)
            && _whitelistSystem.IsWhitelistPass(oreVein.GatherDestructionWhitelist, gathering.Owner))
        {
            oreVein.PreventSpawning = true;
        }
        // End Frontier: gathering changes

        Gather(args.OtherEntity, gathering, gatherable);
        gathering.Comp.Amount--;

        if (gathering.Comp.Amount <= 0)
            QueueDel(gathering);
    }
}

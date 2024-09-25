using Content.Server._NF.Smuggling.Components;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Placeable;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="DeadDropHintVariationPassComponent"/>
public sealed class DeadDropHintVariationPass : VariationPassSystem<DeadDropHintVariationPassComponent>
{
    protected override void ApplyVariation(Entity<DeadDropHintVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        if (HasComp<StationDeadDropHintExemptComponent>(args.Station))
            return;

        // Best query for table-like objects: bonkable filters out grills.
        var query = AllEntityQuery<BonkableComponent, PlaceableSurfaceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var _, out var _, out var xform))
        {
            if (!IsMemberOfStation((uid, xform), ref args))
                continue;

            var prob = Random.NextFloat();
            if (prob < ent.Comp.HintSpawnChance)
            {
                SpawnAttachedTo(ent.Comp.HintSpawnPrototype, xform.Coordinates);
            }
        }
    }
}

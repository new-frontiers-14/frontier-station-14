using Content.Server._NF.Smuggling.Components;
using Content.Server.Construction.Components;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared._NF.BindToGrid;
using Content.Shared.Climbing.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Placeable;

namespace Content.Server.GameTicking.Rules.VariationPass;

public sealed class BindToGridVariationPass : VariationPassSystem<BindToGridVariationPassComponent>
{
    protected override void ApplyVariation(Entity<BindToGridVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        //if (HasComp<StationDeadDropHintExemptComponent>(args.Station))
        //    return;

        var machineQuery = AllEntityQuery<MachineComponent, TransformComponent>();
        while (machineQuery.MoveNext(out var uid, out var _, out var xform))
        {
            if (!IsMemberOfStation((uid, xform), ref args) || HasComp<BindToGridExemptionComponent>(uid))
                continue;

            var binding = EnsureComp<BindToGridComponent>(uid);
            binding.BoundGrid = GetNetEntity(args.Station);
        }
        var boardQuery = AllEntityQuery<MachineBoardComponent, TransformComponent>();
        while (boardQuery.MoveNext(out var uid, out var _, out var xform))
        {
            if (!IsMemberOfStation((uid, xform), ref args) || HasComp<BindToGridExemptionComponent>(uid))
                continue;

            var binding = EnsureComp<BindToGridComponent>(uid);
            binding.BoundGrid = GetNetEntity(args.Station);
        }
    }
}

using Content.Server._NF.BindToStation;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared._NF.BindToStation;

namespace Content.Server._NF.GameTicking.Rules.VariationPass;

public sealed class BindToStationVariationPass : VariationPassSystem<BindToStationVariationPassComponent>
{
    [Dependency] BindToStationSystem _bindToStation = default!;
    protected override void ApplyVariation(Entity<BindToStationVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        // Exempt station?  Don't apply this variation.
        if (HasComp<BindToStationVariationPassExemptionComponent>(args.Station))
            return;

        // Tie vendors to a particular station.
        var vendorQuery = AllEntityQuery<BindToStationComponent, TransformComponent>();
        while (vendorQuery.MoveNext(out var uid, out var bind, out var xform))
        {
            if (!bind.Enabled || !IsMemberOfStation((uid, xform), ref args))
                continue;

            _bindToStation.BindToStation(uid, args.Station);
        }
    }
}

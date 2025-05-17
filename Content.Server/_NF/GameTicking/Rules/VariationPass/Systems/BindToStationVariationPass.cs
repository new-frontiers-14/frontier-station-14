using Content.Server._NF.BindToStation;
using Content.Server.Construction.Components;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared._NF.BindToStation;
using Content.Shared.Construction.Components;
using Content.Shared.VendingMachines;

namespace Content.Server.GameTicking.Rules.VariationPass;

public sealed class BindToStationVariationPass : VariationPassSystem<BindToStationVariationPassComponent>
{
    [Dependency] BindToStationSystem _bindToStation = default!;
    protected override void ApplyVariation(Entity<BindToStationVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        // Exempt station?  Don't apply this variation.
        if (HasComp<BindToStationVariationPassExemptionComponent>(args.Station))
            return;

        // Tie machines to a particular station.
        var machineQuery = AllEntityQuery<MachineComponent, TransformComponent>();
        while (machineQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || !IsMemberOfStation((uid, xform), ref args))
                continue;

            _bindToStation.BindToStation(uid, args.Station);
        }

        var boardQuery = AllEntityQuery<MachineBoardComponent, TransformComponent>();
        while (boardQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || !IsMemberOfStation((uid, xform), ref args))
                continue;

            _bindToStation.BindToStation(uid, args.Station);
        }

        // Tie computers and boards to a particular station.
        var computerQuery = AllEntityQuery<ComputerComponent, TransformComponent>();
        while (computerQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || !IsMemberOfStation((uid, xform), ref args))
                continue;

            _bindToStation.BindToStation(uid, args.Station);
        }

        var compBoardQuery = AllEntityQuery<ComputerBoardComponent, TransformComponent>();
        while (compBoardQuery.MoveNext(out var uid, out var _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || !IsMemberOfStation((uid, xform), ref args))
                continue;

            _bindToStation.BindToStation(uid, args.Station);
        }

        // Tie vendors to a particular station.
        var vendorQuery = AllEntityQuery<VendingMachineComponent, TransformComponent>();
        while (vendorQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || !IsMemberOfStation((uid, xform), ref args))
                continue;

            _bindToStation.BindToStation(uid, args.Station);
        }
    }
}

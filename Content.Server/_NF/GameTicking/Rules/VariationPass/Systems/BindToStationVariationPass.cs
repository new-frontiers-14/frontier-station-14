using Content.Server._NF.BindToStation;
using Content.Server.Anomaly.Components;
using Content.Server.Construction.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared._NF.BindToStation;
using Content.Shared.Construction.Components;
using Content.Shared.VendingMachines;

namespace Content.Server._NF.GameTicking.Rules.VariationPass;

public sealed class BindToStationVariationPass : VariationPassSystem<BindToStationVariationPassComponent>
{
    [Dependency] BindToStationSystem _bindToStation = default!;
    protected override void ApplyVariation(Entity<BindToStationVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        // Exempt station?  Don't apply this variation.
        if (HasComp<BindToStationVariationPassExemptionComponent>(args.Station))
            return;

        // Tie machines to a particular station.
        BindEntitiesToStation<MachineComponent>(ref args);
        BindEntitiesToStation<MachineBoardComponent>(ref args);

        // Tie computers and boards to a particular station.
        BindEntitiesToStation<ComputerComponent>(ref args);
        BindEntitiesToStation<ComputerBoardComponent>(ref args);

        // Tie vendors to a particular station.
        // TODO: swap this over to a separate component.
        BindEntitiesToStation<VendingMachineComponent>(ref args);
        BindEntitiesToStation<AnomalyGeneratorComponent>(ref args);
    }

    /// <summary>
    /// Binds every entity with a particular type of component to a given station.
    /// </summary>
    /// <typeparam name="TComp">The component to check for.</typeparam>
    /// <param name="args">The variation pass arguments, which contains the station to bind to.</param>
    private void BindEntitiesToStation<TComp>(ref StationVariationPassEvent args) where TComp : IComponent
    {
        var vendorQuery = AllEntityQuery<TComp, TransformComponent>();
        while (vendorQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || !IsMemberOfStation((uid, xform), ref args))
                continue;

            _bindToStation.BindToStation(uid, args.Station);
        }
    }
}

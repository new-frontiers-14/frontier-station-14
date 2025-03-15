using Content.Server._NF.BindToStation;
using Content.Server._NF.Station.Components;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._NF.BindToStation;
using Content.Shared.Construction.Components;

namespace Content.Server._NF.Station.Systems;

public sealed class StationBindMachinesSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly BindToStationSystem _bindToStation = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationBindMachinesComponent, StationPostInitEvent>(OnPostInit);
    }

    private void OnPostInit(EntityUid uid, StationBindMachinesComponent component, ref StationPostInitEvent args)
    {
        BindMachinesToStation(uid);
    }

    public void BindMachinesToStation(EntityUid stationUid)
    {
        var machineQuery = AllEntityQuery<MachineComponent, TransformComponent>();
        while (machineQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || _stationSystem.GetOwningStation(uid, xform) != stationUid)
                continue;

            _bindToStation.BindToStation(uid, stationUid);
        }

        var boardQuery = AllEntityQuery<MachineBoardComponent, TransformComponent>();
        while (boardQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || _stationSystem.GetOwningStation(uid, xform) != stationUid)
                continue;

            _bindToStation.BindToStation(uid, stationUid);
        }

        var computerQuery = AllEntityQuery<ComputerComponent, TransformComponent>();
        while (computerQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || _stationSystem.GetOwningStation(uid, xform) != stationUid)
                continue;

            _bindToStation.BindToStation(uid, stationUid);
        }

        var compBoardQuery = AllEntityQuery<ComputerBoardComponent, TransformComponent>();
        while (compBoardQuery.MoveNext(out var uid, out var _, out var xform))
        {
            if (HasComp<BindToStationExemptionComponent>(uid) || _stationSystem.GetOwningStation(uid, xform) != stationUid)
                continue;

            _bindToStation.BindToStation(uid, stationUid);
        }
    }
}

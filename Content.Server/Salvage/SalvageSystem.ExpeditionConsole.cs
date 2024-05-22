using Content.Server.Station.Components;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Procedural;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Content.Server.Salvage.Expeditions;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    [ValidatePrototypeId<EntityPrototype>]
    public const string CoordinatesDisk = "CoordinatesDisk";

    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    private const float ShuttleFTLMassThreshold = 50f;
    private const float ShuttleFTLRange = 150f;

    private void OnSalvageClaimMessage(EntityUid uid, SalvageExpeditionConsoleComponent component, ClaimSalvageMessage args)
    {
        var station = _station.GetOwningStation(uid);

        // Corvax-Frontier Start
        var activeExpeditionCount = 0;
        var expeditionQuery = AllEntityQuery<SalvageExpeditionDataComponent, MetaDataComponent>();
        while (expeditionQuery.MoveNext(out var expeditionUid, out _, out _))
            if (TryComp<SalvageExpeditionDataComponent>(expeditionUid, out var expeditionData) && expeditionData.Claimed)
                activeExpeditionCount++;

        if (activeExpeditionCount >= 15)
        {
            PlayDenySound(uid, component);
            _popupSystem.PopupEntity(Loc.GetString("ftl-channel-blocked"), uid, PopupType.MediumCaution);
            return;
        }
        // Corvax-Frontier End

        if (!TryComp<SalvageExpeditionDataComponent>(station, out var data) || data.Claimed)
            return;

        if (!data.Missions.TryGetValue(args.Index, out var missionparams))
            return;

        // Существующие проверки и логика
        if (!TryComp<StationDataComponent>(station, out var stationData))
            return;
        if (_station.GetLargestGrid(stationData) is not {Valid : true} grid)
            return;
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var xform = Transform(grid);
        var bounds = xform.WorldMatrix.TransformBox(gridComp.LocalAABB).Enlarged(ShuttleFTLRange);
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var other in _mapManager.FindGridsIntersecting(xform.MapID, bounds))
        {
            if (grid == other.Owner ||
                !bodyQuery.TryGetComponent(other.Owner, out var body) ||
                body.Mass < ShuttleFTLMassThreshold)
            {
                continue;
            }

            PlayDenySound(uid, component);
            _popupSystem.PopupEntity(Loc.GetString("shuttle-ftl-proximity"), uid, PopupType.MediumCaution);
            UpdateConsoles(data);
            return;
        }

        // Frontier change - disable coordinate disks for expedition missions
        //var cdUid = Spawn(CoordinatesDisk, Transform(uid).Coordinates);
        SpawnMission(missionparams, station.Value, null);

        data.ActiveMission = args.Index;
        var mission = GetMission(missionparams.MissionType, missionparams.Difficulty, missionparams.Seed);
        data.NextOffer = _timing.CurTime + mission.Duration + TimeSpan.FromSeconds(1);

        // Frontier change - disable coordinate disks for expedition missions
        //_labelSystem.Label(cdUid, GetFTLName(_prototypeManager.Index<DatasetPrototype>("names_borer"), missionparams.Seed));
        //_audio.PlayPvs(component.PrintSound, uid);

        UpdateConsoles(data);
    }

    private void OnSalvageFinishMessage(EntityUid entity, SalvageExpeditionConsoleComponent component, FinishSalvageMessage e)
    {
        if (!TryComp<SalvageExpeditionDataComponent>(_station.GetOwningStation(entity), out var data) || !data.CanFinish)
            return;

        data.CanFinish = false;
        UpdateConsoles(data);

        var map = Transform(entity).MapUid;

        if (!TryComp<SalvageExpeditionComponent>(map, out var expedition))
            return;

        var newEndTime = _timing.CurTime + TimeSpan.FromSeconds(10);

        if (expedition.EndTime <= newEndTime)
            return;

        expedition.EndTime = newEndTime;
        expedition.Stage = ExpeditionStage.FinalCountdown;

        Announce(map.Value, Loc.GetString("salvage-expedition-announcement-early-finish"));
    }

    private void OnSalvageConsoleInit(Entity<SalvageExpeditionConsoleComponent> console, ref ComponentInit args)
    {
        UpdateConsole(console);
    }

    private void OnSalvageConsoleParent(Entity<SalvageExpeditionConsoleComponent> console, ref EntParentChangedMessage args)
    {
        UpdateConsole(console);
    }

    private void UpdateConsoles(SalvageExpeditionDataComponent component)
    {
        var state = GetState(component);

        var query = AllEntityQuery<SalvageExpeditionConsoleComponent, UserInterfaceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var uiComp, out var xform))
        {
            var station = _station.GetOwningStation(uid, xform);

            if (station != component.Owner)
                continue;

            _ui.SetUiState((uid, uiComp), SalvageConsoleUiKey.Expedition, state);
        }
    }

    private void UpdateConsole(Entity<SalvageExpeditionConsoleComponent> component)
    {
        var station = _station.GetOwningStation(component);
        SalvageExpeditionConsoleState state;

        if (TryComp<SalvageExpeditionDataComponent>(station, out var dataComponent))
        {
            state = GetState(dataComponent);
        }
        else
        {
            state = new SalvageExpeditionConsoleState(TimeSpan.Zero, false, true, false, 0, new List<SalvageMissionParams>());
        }

        _ui.SetUiState(component.Owner, SalvageConsoleUiKey.Expedition, state);
    }

    private void PlayDenySound(EntityUid uid, SalvageExpeditionConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
    }
}

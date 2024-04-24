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

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    [ValidatePrototypeId<EntityPrototype>]
    public const string CoordinatesDisk = "CoordinatesDisk";

    public SalvageMissionParams MissionParams = default!;

    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    private const float ShuttleFTLMassThreshold = 50f;
    private const float ShuttleFTLRange = 150f;

    private void OnSalvageClaimMessage(EntityUid uid, SalvageExpeditionConsoleComponent component, ClaimSalvageMessage args)
    {
        var activeExpeditionCount = CountActiveExpeditions();

        if (activeExpeditionCount >= 2)
        {
            HandleExpeditionLimitReached(uid, component);
            return;
        }

        if (!TryGetStationData(uid, out var data) || data.Claimed)
            return;

        if (!data.Missions.TryGetValue(args.Index, out var missionParams))
            return;

        if (!TryGetStationAndGridComponents(uid, out var station, out var grid))
            return;

        if (CheckProximityToOtherObjects(uid, grid))
            return;

        SpawnMissionAndHandleData(data, missionParams, station);
    }

    private int CountActiveExpeditions()
    {
        var activeExpeditionCount = 0;
        var expeditionQuery = EntityManager.AllEntityQueryEnumerator<SalvageExpeditionDataComponent, MetaDataComponent>();

        while (expeditionQuery.MoveNext(out _, out _, out var expeditionData))
        {
            if (expeditionData != null && !expeditionData.Claimed)
            {
                activeExpeditionCount++;
            }
        }

        return activeExpeditionCount;
    }

    private void HandleExpeditionLimitReached(EntityUid uid, SalvageExpeditionConsoleComponent component)
    {
        PlayDenySound(uid, component);
        _popupSystem.PopupEntity(Loc.GetString("ftl-channel-blocked"), uid, PopupType.MediumCaution);
    }

    private bool TryGetStationData(EntityUid uid, out SalvageExpeditionDataComponent data)
    {
        var station = _station.GetOwningStation(uid);
        return TryComp(station, out data);
    }

    private bool TryGetStationAndGridComponents(EntityUid uid, out SalvageExpeditionDataComponent station, out MapGridComponent grid)
    {
        var station = _station.GetOwningStation(uid);
        if (!TryComp(station, out station))
        {
            grid = null;
            return false;
        }

        var gridEntity = _station.GetLargestGrid(station);
        if (gridEntity is not { Valid: true })
        {
            grid = null;
            return false;
        }

        return TryComp(gridEntity, out grid);
    }

    private bool CheckProximityToOtherObjects(EntityUid uid, MapGridComponent grid)
    {
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
            return true;
        }

        return false;
    }

    private void SpawnMissionAndHandleData(SalvageExpeditionDataComponent data, MissionParams missionParams, SalvageExpeditionConsoleComponent station)
    {
        SpawnMission(missionParams, station.Value, null);
        data.ActiveMission = args.Index;
        var mission = GetMission(missionParams.MissionType, missionParams.Difficulty, missionParams.Seed);
        data.NextOffer = _timing.CurTime + mission.Duration + TimeSpan.FromSeconds(1);
        UpdateConsoles(data);
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

            _ui.TrySetUiState(uid, SalvageConsoleUiKey.Expedition, state, ui: uiComp);
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
            state = new SalvageExpeditionConsoleState(TimeSpan.Zero, false, true, 0, new List<SalvageMissionParams>());
        }

        _ui.TrySetUiState(component, SalvageConsoleUiKey.Expedition, state);
    }

    private void PlayDenySound(EntityUid uid, SalvageExpeditionConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
    }
}

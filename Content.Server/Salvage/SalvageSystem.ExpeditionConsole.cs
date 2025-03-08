using Content.Shared.Shuttles.Components;
using Content.Shared.Procedural;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Content.Shared._NF.CCVar;
using Content.Server.Station.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    [ValidatePrototypeId<EntityPrototype>]
    public const string CoordinatesDisk = "CoordinatesDisk";

    [Dependency] private readonly SharedPopupSystem _popupSystem = default!; // Frontier

    private const float ShuttleFTLMassThreshold = 50f; // Frontier
    private const float ShuttleFTLRange = 150f; // Frontier

    private void OnSalvageClaimMessage(EntityUid uid, SalvageExpeditionConsoleComponent component, ClaimSalvageMessage args)
    {
        var station = _station.GetOwningStation(uid);

        if (!TryComp<SalvageExpeditionDataComponent>(station, out var data) || data.Claimed)
            return;

        if (!data.Missions.TryGetValue(args.Index, out var missionparams))
            return;

        // Frontier: prevent expeditions if there are too many out already.
        var activeExpeditionCount = 0;
        var expeditionQuery = AllEntityQuery<SalvageExpeditionDataComponent, MetaDataComponent>();
        while (expeditionQuery.MoveNext(out var expeditionUid, out _, out _))
            if (TryComp<SalvageExpeditionDataComponent>(expeditionUid, out var expeditionData) && expeditionData.Claimed)
                activeExpeditionCount++;

        if (activeExpeditionCount >= _configurationManager.GetCVar(NFCCVars.SalvageExpeditionMaxActive))
        {
            PlayDenySound(uid, component);
            _popupSystem.PopupEntity(Loc.GetString("shuttle-ftl-too-many"), uid, PopupType.MediumCaution);
            UpdateConsoles(station.Value, data);
            return;
        }
        // End Frontier

        // var cdUid = Spawn(CoordinatesDisk, Transform(uid).Coordinates); // Frontier: no disk-based FTL
        // SpawnMission(missionparams, station.Value, cdUid); // Frontier: no disk-based FTL

        // Frontier: FTL travel is currently restricted to expeditions and such, and so we need to put this here
        // until FTL changes for us in some way.

        // Run a proximity check (unless using a debug console)
        if (!component.Debug)
        {
            if (!TryComp<StationDataComponent>(station, out var stationData)
                || _station.GetLargestGrid(stationData) is not { Valid: true } ourGrid
                || !TryComp<MapGridComponent>(ourGrid, out var gridComp))
            {
                PlayDenySound(uid, component);
                _popupSystem.PopupEntity(Loc.GetString("shuttle-ftl-invalid"), uid, PopupType.MediumCaution);
                UpdateConsoles(station.Value, data);
                return;
            }

            if (HasComp<FTLComponent>(ourGrid))
            {
                PlayDenySound(uid, component);
                _popupSystem.PopupEntity(Loc.GetString("shuttle-ftl-recharge"), uid, PopupType.MediumCaution);
                UpdateConsoles(station.Value, data); // Sure, why not?
                return;
            }

            var xform = Transform(ourGrid);
            var bounds = _transform.GetWorldMatrix(ourGrid).TransformBox(gridComp.LocalAABB).Enlarged(ShuttleFTLRange);
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var otherGrids = new List<Entity<MapGridComponent>>();
            _mapManager.FindGridsIntersecting(xform.MapID, bounds, ref otherGrids);
            foreach (var otherGrid in otherGrids)
            {
                if (ourGrid == otherGrid.Owner ||
                    !bodyQuery.TryGetComponent(otherGrid.Owner, out var body) ||
                    body.Mass < ShuttleFTLMassThreshold)
                {
                    continue;
                }

                PlayDenySound(uid, component);
                _popupSystem.PopupEntity(Loc.GetString("shuttle-ftl-proximity"), uid, PopupType.MediumCaution);
                UpdateConsoles(station.Value, data);
                return;
            }
        }
        SpawnMission(missionparams, station.Value, null);
        // End Frontier

        data.ActiveMission = args.Index;
        var mission = GetMission(missionparams.MissionType, _prototypeManager.Index<SalvageDifficultyPrototype>(missionparams.Difficulty), missionparams.Seed); // Frontier: add MissionType
        data.NextOffer = _timing.CurTime + mission.Duration + TimeSpan.FromSeconds(1);

        _labelSystem.Label(cdUid, GetFTLName(_prototypeManager.Index<LocalizedDatasetPrototype>("NamesBorer"), missionparams.Seed));
        _audio.PlayPvs(component.PrintSound, uid);

        UpdateConsoles((station.Value, data));
    }

    private void OnSalvageConsoleInit(Entity<SalvageExpeditionConsoleComponent> console, ref ComponentInit args)
    {
        UpdateConsole(console);
    }

    private void OnSalvageConsoleParent(Entity<SalvageExpeditionConsoleComponent> console, ref EntParentChangedMessage args)
    {
        UpdateConsole(console);
    }

    private void UpdateConsoles(Entity<SalvageExpeditionDataComponent> component)
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
            state = new SalvageExpeditionConsoleState(TimeSpan.Zero, false, true, 0, new List<SalvageMissionParams>(), component.CanFinish); // Frontier: add CanFinish
        }

        _ui.SetUiState(component.Owner, SalvageConsoleUiKey.Expedition, state);
    }

    // Frontier: deny sound
    private void PlayDenySound(Entity<SalvageExpeditionConsoleComponent> ent)
    {
        _audio.PlayPvs(_audio.GetSound(ent.Comp.ErrorSound), ent);
    }
    // End Frontier
}

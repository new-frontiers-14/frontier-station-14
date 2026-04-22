// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Server.Popups;
using Content.Server.Station.Components;
using Content.Goobstation.Server.MobCaller;
using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Spawners;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;

namespace Content.Goobstation.Server.SpaceWhale.StationProximity;

/// <summary>
/// Hardcoded to space whale spawn
/// </summary>
public sealed class StationProximitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _moveSpeed = default!;

    private bool _spaceWhaleEnabled;
    private float _spaceWhaleSpawnDistance = 2000f;

    private static readonly TimeSpan CheckDelay = TimeSpan.FromSeconds(60);
    private TimeSpan _nextCheck = TimeSpan.Zero;

    private readonly Dictionary<MapId, HashSet<Entity<MapGridComponent, TransformComponent>>> _stations = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceWhaleTargetComponent, MobStateChangedEvent>(OnTargetDeath);
        SubscribeLocalEvent<SpaceWhaleTargetComponent, ComponentShutdown>(OnTargetShutdown);

        Subs.CVar(_cfg, GoobCVars.SpaceWhaleSpawn, x => _spaceWhaleEnabled = x, true);
        Subs.CVar(_cfg, GoobCVars.SpaceWhaleSpawnDistance, x => _spaceWhaleSpawnDistance = x, true);
    }

    private void OnTargetDeath(Entity<SpaceWhaleTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        RemComp<SpaceWhaleTargetComponent>(ent.Owner);
    }

    private void OnTargetShutdown(Entity<SpaceWhaleTargetComponent> ent, ref ComponentShutdown args)
    {
        StopFollowing(ent.Owner);
    }

    private void StopFollowing(Entity<SpaceWhaleTargetComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (TryComp<MobCallerComponent>(ent.Comp.MobCaller, out var caller))
        {
            foreach (var item in caller.SpawnedEntities)
            {
                EnsureComp<TimedDespawnComponent>(item).Lifetime = 15f;
                _moveSpeed.ChangeBaseSpeed(item, 11, 30, 1);
                _moveSpeed.RefreshMovementSpeedModifiers(item);
            }
        }

        QueueDel(ent.Comp.MobCaller);
        ent.Comp.MobCaller = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime > _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckDelay;
        CheckStationProximity();
    }

    private void CheckStationProximity()
    {
        if (!_spaceWhaleEnabled)
            return;

        var stationQuery = EntityQueryEnumerator<BecomesStationComponent, MapGridComponent, TransformComponent>();
        _stations.Clear();

        while (stationQuery.MoveNext(out var uid, out _, out var grid, out var xform))
        {
            if (_stations.TryGetValue(xform.MapID, out var stations))
                stations.Add((uid, grid, xform));
            else
                _stations[xform.MapID] = [(uid, grid, xform)];
        }

        if (_stations.Count == 0)
            return;

        var humanoidQuery = EntityQueryEnumerator<HumanoidProfileComponent, MobStateComponent, TransformComponent>();
        while (humanoidQuery.MoveNext(out var uid, out _, out var mobState, out var xform))
        {
            if (mobState.CurrentState != MobState.Alive)
                continue;

            CheckHumanoidProximity((uid, xform));
        }
    }

    private void CheckHumanoidProximity(Entity<TransformComponent> ent)
    {
        if (!_stations.TryGetValue(ent.Comp.MapID, out var stations))
            return;

        if (ent.Comp.GridUid is { } gridUid && stations.Any(x => x.Owner == gridUid))
        {
            RemCompDeferred<SpaceWhaleTargetComponent>(ent);
            return;
        }

        var humanoidWorldPos = _transform.GetWorldPosition(ent.Comp);
        var closestDistance = float.MaxValue;

        foreach (var (_, grid, xform) in stations)
        {
            var stationWorldPos = _transform.GetWorldPosition(xform);
            var distance = (humanoidWorldPos - stationWorldPos).Length();

            if (grid.LocalAABB.Size.Length() > 0)
            {
                // it needs to be halved to get correct measurements
                var gridRadius = grid.LocalAABB.Size.Length() / 2f;
                distance = Math.Max(0, distance - gridRadius);
            }

            closestDistance = Math.Min(closestDistance, distance);
        }

        if (closestDistance <= _spaceWhaleSpawnDistance)
            RemCompDeferred<SpaceWhaleTargetComponent>(ent);
        else
            HandleFarFromStation(ent);
    }

    private void HandleFarFromStation(EntityUid ent)
    {
        var targetComp = EnsureComp<SpaceWhaleTargetComponent>(ent);

        if (Exists(targetComp.MobCaller))
            return;

        _popup.PopupEntity(
            Loc.GetString("station-proximity-far-from-station"),
            ent,
            ent,
            PopupType.LargeCaution);

        _audio.PlayEntity(new SoundPathSpecifier("/Audio/_Goobstation/Ambience/SpaceWhale/leviathan-appear.ogg"),
            ent,
            ent,
            AudioParams.Default.WithVolume(1f));

        targetComp.MobCaller = SpawnAttachedTo(targetComp.MobCallerProto, ent.ToCoordinates());
    }
}

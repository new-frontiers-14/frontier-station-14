using System.Numerics;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Salvage;
using Content.Server.Salvage.Magnet;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceErrorRule : StationEventSystem<BluespaceErrorRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;

    private List<(Entity<TransformComponent> Entity, EntityUid MapUid, Vector2 LocalPosition)> _playerMobs = new();

    protected override void Started(EntityUid uid, BluespaceErrorRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        if (!_map.TryLoad(shuttleMap, component.GridPath, out var gridUids, options))
            return;
        component.GridUid = gridUids[0];
        if (component.GridUid is not EntityUid gridUid)
            return;
        component.startingValue = _pricing.AppraiseGrid(gridUid);
        _shuttle.SetIFFColor(gridUid, component.Color);
        var offset = _random.NextVector2(1350f, 2200f);
        var mapId = GameTicker.DefaultMap;
        var mapUid = _mapManager.GetMapEntityId(mapId);
        if (TryComp<ShuttleComponent>(component.GridUid, out var shuttle))
        {
            _shuttle.FTLToCoordinates(gridUid, shuttle, new EntityCoordinates(mapUid, offset), 0f, 0f, 30f);
        }

    }

    protected override void Ended(EntityUid uid, BluespaceErrorRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if(!EntityManager.TryGetComponent<TransformComponent>(component.GridUid, out var gridTransform))
        {
            Log.Error("bluespace error objective was missing transform component");
            return;
        }

        if (gridTransform.GridUid is not EntityUid gridUid)
        {
            Log.Error( "bluespace error has no associated grid?");
            return;
        }

        var gridValue = _pricing.AppraiseGrid(gridUid, null);

        var mobQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();
        _playerMobs.Clear();

        while (mobQuery.MoveNext(out var mobUid, out _, out _, out var xform))
        {
            if (xform.GridUid == null || xform.MapUid == null || xform.GridUid != gridUid)
                continue;

            // Can't parent directly to map as it runs grid traversal.
            _playerMobs.Add(((mobUid, xform), xform.MapUid.Value, _transform.GetWorldPosition(xform)));
            _transform.DetachParentToNull(mobUid, xform);
        }

        // Deletion has to happen before grid traversal re-parents players.
        Del(gridUid);

        foreach (var mob in _playerMobs)
        {
            _transform.SetCoordinates(mob.Entity.Owner, new EntityCoordinates(mob.MapUid, mob.LocalPosition));
        }


        var query = EntityQuery<StationBankAccountComponent>();
        foreach (var account in query)
        {
            _cargo.DeductFunds(account, (int) -(gridValue * component.RewardFactor));
        }
    }
}


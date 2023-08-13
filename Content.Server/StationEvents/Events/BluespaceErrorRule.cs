using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Salvage;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.StationEvents.Components;
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

    private EntityUid _objective = new();

    protected override void Started(EntityUid uid, BluespaceErrorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        if (!_map.TryLoad(shuttleMap, component.GridPath, out var gridUids, options))
            return;
        _objective = gridUids[0];
        _shuttle.SetIFFColor(_objective, component.Color);
        var offset = _random.NextVector2(1350f, 2200f);
        var mapId = GameTicker.DefaultMap;
        var coords = new MapCoordinates(offset, mapId);
        var location = Spawn(null, coords);
        if (TryComp<ShuttleComponent>(_objective, out var shuttle))
        {
            _shuttle.FTLTravel(_objective, shuttle, location, 5.5f, 55f);
        }

    }

    protected override void Ended(EntityUid uid, BluespaceErrorRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if(!EntityManager.TryGetComponent<TransformComponent>(_objective, out var gridTransform))
        {
            Log.Error("bluespace error objective was missing transform component");
            return;
        }

        if (gridTransform.GridUid == null)
        {
            Log.Error( "bluespace error has no associated grid?");
            return;
        }
        var gridValue = _pricing.AppraiseGrid(_objective, null);
        foreach (var player in Filter.Empty().AddInGrid(gridTransform.GridUid.Value, EntityManager).Recipients)
        {
            if (player.AttachedEntity.HasValue)
            {
                var playerEntityUid = player.AttachedEntity.Value;
                if (HasComp<SalvageMobRestrictionsComponent>(playerEntityUid))
                {
                    // Salvage mobs are NEVER immune (even if they're from a different salvage, they shouldn't be here)
                    continue;
                }
                _transform.SetParent(playerEntityUid, gridTransform.ParentUid);
            }
        }
        // Deletion has to happen before grid traversal re-parents players.
        Del(_objective);
        var query = EntityQuery<StationBankAccountComponent>();
        foreach (var account in query)
        {
            _cargo.DeductFunds(account, (int) -(gridValue * component.RewardFactor));
        }
    }
}


/// using System.Linq;
/// using System.Numerics;
/// using Content.Server._NF.GameRule.Events.Components;
/// using Content.Server.Cargo.Components;
/// using Content.Server.Cargo.Systems;
/// using Robust.Server.GameObjects;
/// using Robust.Server.Maps;
/// using Robust.Shared.Map;
/// using Content.Server.GameTicking.Rules.Components;
/// using Content.Server.Shuttles.Components;
/// using Content.Server.Shuttles.Systems;
/// using Content.Server.StationEvents.Events;
/// using Content.Shared.Humanoid;
/// using Content.Shared.Mobs.Components;
/// using Content.Shared.Shipyard.Prototypes;
/// using Robust.Shared.Prototypes;
/// using Robust.Shared.Random;

/// namespace Content.Server._NF.GameRule.Events;

/// public sealed class RandomShipyardErrorRule : StationEventSystem<RandomShipyardErrorRuleComponent>
/// {
///     [Dependency] private readonly IMapManager _mapManager = default!;
///     [Dependency] private readonly MapLoaderSystem _map = default!;
///     [Dependency] private readonly ShuttleSystem _shuttle = default!;
///     [Dependency] private readonly IRobustRandom _random = default!;
///     [Dependency] private readonly SharedTransformSystem _transform = default!;
///     [Dependency] private readonly PricingSystem _pricing = default!;
///     [Dependency] private readonly CargoSystem _cargo = default!;
///     [Dependency] private readonly IPrototypeManager _proto = default!;

///     private List<(Entity<TransformComponent> Entity, EntityUid MapUid, Vector2 LocalPosition)> _playerMobs = new();

///     protected override void Started(EntityUid uid, RandomShipyardErrorRuleComponent component,
///         GameRuleComponent gameRule,
///         GameRuleStartedEvent args)
///     {
///         base.Started(uid, component, gameRule, args);
///         var vesselPrototypes = _proto.EnumeratePrototypes<VesselPrototype>();
///         var prototypes = vesselPrototypes as VesselPrototype[] ?? vesselPrototypes.ToArray();
///         var rnd = new Random();
///         var vesselCount = rnd.Next(component.MinGridsCount, component.MaxGridsCount);

///         for (var i = 0; i < vesselCount; i++)
///         {
///             var index = rnd.Next(0, prototypes.Count());
///             var gridPath = prototypes.ElementAt(index).ShuttlePath.ToString();
///             var shuttleMap = _mapManager.CreateMap();
///             var options = new MapLoadOptions
///             {
///                 LoadMap = true,
///             };
///             if (!_map.TryLoad(shuttleMap, gridPath, out var gridUids, options))
///                 return;
///             component.GridUids.Add(gridUids[0]);
///             if (component.GridUids.Last() is not { } gridUid)
///                 return;
///             _shuttle.SetIFFColor(gridUid, new Color(rnd.Next(256), rnd.Next(256), rnd.Next(256), 0));
///             var offset = _random.NextVector2(1800f, 2000f);
///             var mapId = GameTicker.DefaultMap;
///             var coords = new MapCoordinates(offset, mapId);
///             var location = Spawn(null, coords);
///             if (TryComp<ShuttleComponent>(component.GridUids.Last(), out var shuttle))
///             {
///                 _shuttle.FTLTravel(gridUid, shuttle, location, 5.5f, 55f);
///             }
///         }
///     }

///     protected override void Ended(
///         EntityUid uid,
///         RandomShipyardErrorRuleComponent component,
///         GameRuleComponent gameRule,
///         GameRuleEndedEvent args)
///     {
///         base.Ended(uid, component, gameRule, args);
///         foreach (var gridId in component.GridUids)
///         {


///             if (!EntityManager.TryGetComponent<TransformComponent>(gridId, out var gridTransform))
///             {
///                 Log.Error("bluespace error objective was missing transform component");
///                 return;
///             }

///             if (gridTransform.GridUid is not EntityUid gridUid)
///             {
///                 Log.Error("bluespace error has no associated grid?");
///                 return;
///             }

///             var gridValue = _pricing.AppraiseGrid(gridUid, null);

///             var mobQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();
///             _playerMobs.Clear();

///             while (mobQuery.MoveNext(out var mobUid, out _, out _, out var xform))
///             {
///                 if (xform.GridUid == null || xform.MapUid == null || xform.GridUid != gridUid)
///                     continue;

///                 // Can't parent directly to map as it runs grid traversal.
///                 _playerMobs.Add(((mobUid, xform), xform.MapUid.Value, _transform.GetWorldPosition(xform)));
///                 _transform.DetachParentToNull(mobUid, xform);
///             }

///             // Deletion has to happen before grid traversal re-parents players.
///             Del(gridUid);

///             foreach (var mob in _playerMobs)
///             {
///                 _transform.SetCoordinates(mob.Entity.Owner, new EntityCoordinates(mob.MapUid, mob.LocalPosition));
///             }


///             var query = EntityQuery<StationBankAccountComponent>();
///             foreach (var account in query)
///             {
///                 _cargo.DeductFunds(account, (int) -gridValue);
///             }
///         }
///     }
/// }

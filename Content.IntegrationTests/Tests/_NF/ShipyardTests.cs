﻿using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared.Shipyard.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._NF;

[TestFixture]
public sealed class ShipyardTest
{
    [Test]
    public async Task CheckAllShuttleGrids()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var mapLoader = entManager.System<MapLoaderSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();

        await server.WaitPost(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var vessel in protoManager.EnumeratePrototypes<VesselPrototype>())
                {
                    var mapId = mapManager.CreateMap();

                    try
                    {
                        Assert.That(mapLoader.TryLoad(mapId, vessel.ShuttlePath.ToString(), out var roots));
                        Assert.That(roots.Where(uid => entManager.HasComponent<MapGridComponent>(uid)), Is.Not.Empty);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to load shuttle {vessel.ShuttlePath}", ex);
                    }

                    try
                    {
                        mapManager.DeleteMap(mapId);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to delete map {vessel.ShuttlePath}", ex);
                    }
                }
            });
        });
        await server.WaitRunTicks(1);
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NoShipyardShipArbitrage()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var pricing = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PricingSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var vessel in protoManager.EnumeratePrototypes<VesselPrototype>())
                {
                    var mapId = mapManager.CreateMap();
                    double combinedPrice = 0;

                    Assert.That(mapLoader.TryLoad(mapId, vessel.ShuttlePath.ToString(), out var roots));
                    var shuttle = roots.FirstOrDefault(uid => entManager.HasComponent<MapGridComponent>(uid));

                    pricing.AppraiseGrid(shuttle, null, (uid, price) =>
                    {
                        combinedPrice += price;
                    });

                    Assert.That(combinedPrice, Is.AtMost(vessel.Price),
                        $"Found arbitrage on {vessel.ID} shuttle! Cost is {vessel.Price} but sell is {combinedPrice}!");
                    Assert.That(vessel.Price - combinedPrice, Is.GreaterThan(vessel.Price * 0.05),
                        $"Arbitrage possible on {vessel.ID}. {vessel.Price} - {combinedPrice} = {vessel.Price - combinedPrice} > 5% of the buy price!");

                    mapManager.DeleteMap(mapId);
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

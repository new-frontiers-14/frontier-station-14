using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared._NF.Shipyard.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
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
        var map = entManager.System<MapSystem>();

        await server.WaitPost(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var vessel in protoManager.EnumeratePrototypes<VesselPrototype>())
                {
                    map.CreateMap(out var mapId);

                    try
                    {
                        Assert.That(mapLoader.TryLoadGrid(mapId, vessel.ShuttlePath, out var shuttle));
                        Assert.That(shuttle.HasValue, Is.True);
                        Assert.That(entManager.HasComponent<MapGridComponent>(shuttle.Value), Is.True);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to load shuttle {vessel.ShuttlePath}", ex);
                    }

                    try
                    {
                        map.DeleteMap(mapId);
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
        var map = entManager.System<MapSystem>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var pricing = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PricingSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var vessel in protoManager.EnumeratePrototypes<VesselPrototype>())
                {
                    map.CreateMap(out var mapId);
                    double appraisePrice = 0;

                    Assert.That(mapLoader.TryLoadGrid(mapId, vessel.ShuttlePath, out var shuttle));
                    Assert.That(entManager.HasComponent<MapGridComponent>(shuttle.Value), Is.True);

                    pricing.AppraiseGrid(shuttle.Value, null, (uid, price) =>
                    {
                        appraisePrice += price;
                    });

                    var idealMinPrice = appraisePrice * vessel.MinPriceMarkup;

                    Assert.That(vessel.Price, Is.AtLeast(idealMinPrice),
                        $"Arbitrage possible on {vessel.ID}. Minimal price should be {idealMinPrice}, {(vessel.MinPriceMarkup - 1.0f) * 100}% over the appraise price ({appraisePrice}).");

                    map.DeleteMap(mapId);
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Cargo.Components;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class CargoTest
{
    [Test]
    public async Task NoCargoOrderArbitrage()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var pricing = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PricingSystem>();

        await server.WaitAssertion(() =>
        {
            var mapId = testMap.MapId;

            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<CargoProductPrototype>())
                {
                    var ent = entManager.SpawnEntity(proto.Product, new MapCoordinates(Vector2.Zero, mapId));
                    var price = pricing.GetPrice(ent);

                    Assert.That(price, Is.AtMost(proto.PointCost), $"Found arbitrage on {proto.ID} cargo product! Cost is {proto.PointCost} but sell is {price}!");
                    entManager.DeleteEntity(ent);
                }
            });

            mapManager.DeleteMap(mapId);
        });

        await pair.CleanReturnAsync();
    }
    [Test]
    public async Task NoCargoBountyArbitageTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var cargo = entManager.System<CargoSystem>();

        var bounties = protoManager.EnumeratePrototypes<CargoBountyPrototype>().ToList();

        await server.WaitAssertion(() =>
        {
            var mapId = testMap.MapId;

            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<CargoProductPrototype>())
                {
                    var ent = entManager.SpawnEntity(proto.Product, new MapCoordinates(Vector2.Zero, mapId));

                    foreach (var bounty in bounties)
                    {
                        if (cargo.IsBountyComplete(ent, bounty))
                            Assert.That(proto.PointCost, Is.GreaterThan(bounty.Reward), $"Found arbitrage on {bounty.ID} cargo bounty! Product {proto.ID} costs {proto.PointCost} but fulfills bounty {bounty.ID} with reward {bounty.Reward}!");
                    }

                    entManager.DeleteEntity(ent);
                }
            });

            mapManager.DeleteMap(mapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NoStaticPriceAndStackPrice()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var mapId = testMap.MapId;
            var grid = mapManager.CreateGrid(mapId);
            var coord = new EntityCoordinates(grid.Owner, 0, 0);

            var protoIds = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => !p.Components.ContainsKey("MapGrid")) // Grids are not for sale.
                .Select(p => p.ID)
                .ToList();

            foreach (var proto in protoIds)
            {
                var ent = entManager.SpawnEntity(proto, coord);

                if (entManager.TryGetComponent<StackPriceComponent>(ent, out var stackpricecomp)
                    && stackpricecomp.Price > 0)
                {
                    if (entManager.TryGetComponent<StaticPriceComponent>(ent, out var staticpricecomp))
                    {
                        Assert.That(staticpricecomp.Price, Is.EqualTo(0),
                            $"The prototype {proto} has a StackPriceComponent and StaticPriceComponent whose values are not compatible with each other.");
                    }
                }

                if (entManager.HasComponent<StackComponent>(ent))
                {
                    if (entManager.TryGetComponent<StaticPriceComponent>(ent, out var staticpricecomp))
                    {
                        Assert.That(staticpricecomp.Price, Is.EqualTo(0),
                            $"The prototype {proto} has a StackComponent and StaticPriceComponent whose values are not compatible with each other.");
                    }
                }

                entManager.DeleteEntity(ent);
            }
            mapManager.DeleteMap(mapId);
        });

        await pair.CleanReturnAsync();
    }


    [TestPrototypes]
    private const string StackProto = @"
- type: entity
  id: A

- type: stack
  id: StackProto
  spawn: A

- type: entity
  id: StackEnt
  components:
  - type: StackPrice
    price: 20
  - type: Stack
    stackType: StackProto
    count: 5
";

    [Test]
    public async Task StackPrice()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var priceSystem = entManager.System<PricingSystem>();

        var ent = entManager.SpawnEntity("StackEnt", MapCoordinates.Nullspace);
        var price = priceSystem.GetPrice(ent);
        Assert.That(price, Is.EqualTo(100.0));

        await pair.CleanReturnAsync();
    }
}

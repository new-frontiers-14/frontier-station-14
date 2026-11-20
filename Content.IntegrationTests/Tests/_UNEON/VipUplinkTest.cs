using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Shared._UNEON.VipUplink;
using Content.Shared.CartridgeLoader;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._UNEON;

[TestFixture]
public sealed class VipUplinkTest
{
    [Test]
    public async Task PdaInstallsVipProgramAndCatalogueSpawnsFullStacks()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var entities = server.EntMan;
            var cartridges = entities.System<CartridgeLoaderSystem>();
            var pda = entities.SpawnEntity("PassengerPDA", map.GridCoords);
            Assert.That(cartridges.TryGetProgram<VipUplinkCartridgeComponent>(pda, out var program), Is.True);
            var denied = new CartridgeProgramAccessAttemptEvent(pda); // No authenticated player session.
            entities.EventBus.RaiseLocalEvent(program!.Value, denied);
            Assert.That(denied.Cancelled, Is.True);
            Assert.That(entities.GetComponent<CartridgeComponent>(program.Value).InstallationStatus,
                Is.EqualTo(InstallationStatus.Readonly));
            Assert.That(cartridges.GetInstalled(pda).Count(uid => entities.HasComponent<VipUplinkCartridgeComponent>(uid)),
                Is.EqualTo(1));
            var products = server.ProtoMan.EnumeratePrototypes<VipUplinkProductPrototype>().ToArray();
            Assert.That(products.Select(p => p.ID), Is.SupersetOf(new[] { "UneonVipSteel", "UneonVipGlass", "UneonVipPlastic" }));
            foreach (var id in new[] { "UneonVipSteel", "UneonVipGlass", "UneonVipPlastic" })
            {
                var product = products.Single(p => p.ID == id);
                var item = entities.Spawn(product.Product);
                Assert.That(entities.GetComponent<TransformComponent>(item).MapID, Is.EqualTo(MapId.Nullspace));
                entities.System<SharedTransformSystem>().SetCoordinates(item, map.GridCoords);
                Assert.That(entities.GetComponent<TransformComponent>(item).MapID, Is.EqualTo(map.MapId));
                Assert.That(entities.GetComponent<StackComponent>(item).Count, Is.EqualTo(30));
                entities.DeleteEntity(item);
            }
            entities.DeleteEntity(pda);
        });
        await pair.CleanReturnAsync();
    }
}

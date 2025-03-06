using Content.Server.Salvage.Magnet;
using Content.Server.Solar.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._NF;

[TestFixture]
public sealed class IllegalComponentTest
{
    // A list of components to check all entity prototypes for.
    private static readonly Type[] IllegalComponents =
    {
        typeof(SolarPanelComponent), // Frontier: use NF variant
        typeof(SalvageMobRestrictionsComponent), // Frontier: use NF variant
    };

    [Test]
    public async Task CheckServerIllegalComponents()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var entity in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    foreach (var component in IllegalComponents)
                    {
                        Assert.That(entity.HasComponent(component), Is.False, $"Entity {entity} contains illegal component {component}.");
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Chemistry
{
    [TestFixture]
    [TestOf(typeof(ReactionPrototype))]
    public sealed class TryAllReactionsTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  id: TestSolutionContainer
  components:
  - type: SolutionContainerManager
    solutions:
      beaker:
        maxVol: 50
        canMix: true";

        [Test]
        public async Task TryAllTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;
            var solutionSystem = server.ResolveDependency<IEntitySystemManager>()
                .GetEntitySystem<SolutionContainerSystem>();

            foreach (var reactionPrototype in prototypeManager.EnumeratePrototypes<ReactionPrototype>())
            {
                //since i have no clue how to isolate each loop assert-wise im just gonna throw this one in for good measure
                Console.WriteLine($"Testing {reactionPrototype.ID}");

                EntityUid beaker = default;
                Solution component = null;

                await server.WaitAssertion(() =>
                {
                    beaker = entityManager.SpawnEntity("TestSolutionContainer", coordinates);
                    Assert.That(solutionSystem
                        .TryGetSolution(beaker, "beaker", out component));
                    foreach (var (id, reactant) in reactionPrototype.Reactants)
                    {
#pragma warning disable NUnit2045
                        Assert.That(solutionSystem
                            .TryAddReagent(beaker, component, id, reactant.Amount, out var quantity));
                        Assert.That(reactant.Amount, Is.EqualTo(quantity));
#pragma warning restore NUnit2045
                    }

                    solutionSystem.SetTemperature(beaker, component, reactionPrototype.MinimumTemperature);

                    if (reactionPrototype.MixingCategories != null)
                    {
                        var dummyEntity = entityManager.SpawnEntity(null, MapCoordinates.Nullspace);
                        var mixerComponent = entityManager.AddComponent<ReactionMixerComponent>(dummyEntity);
                        mixerComponent.ReactionTypes = reactionPrototype.MixingCategories;
                        solutionSystem.UpdateChemicals(beaker, component, true, mixerComponent);
                    }
                });

                await server.WaitIdleAsync();

                await server.WaitAssertion(() =>
                {
                    //you just got linq'd fool
                    //(i'm sorry)
                    var foundProductsMap = reactionPrototype.Products
                        .Concat(reactionPrototype.Reactants.Where(x => x.Value.Catalyst).ToDictionary(x => x.Key, x => x.Value.Amount))
                        .ToDictionary(x => x, _ => false);
                    foreach (var (reagent, quantity) in component.Contents)
                    {
                        Assert.That(foundProductsMap.TryFirstOrNull(x => x.Key.Key == reagent.Prototype && x.Key.Value == quantity, out var foundProduct));
                        foundProductsMap[foundProduct.Value.Key] = true;
                    }

                    Assert.That(foundProductsMap.All(x => x.Value));
                });

            }
            await pair.CleanReturnAsync();
        }
    }

}

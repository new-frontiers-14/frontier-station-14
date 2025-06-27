using System.Collections.Generic;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._NF;

[TestFixture]
public sealed class TechnologyTreeTests
{
    [Test]
    public async Task CheckDuplicateTechPositions()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        Dictionary<Vector2i, string> techNamesByPosition = new();

        await server.WaitPost(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var tech in protoManager.EnumeratePrototypes<TechnologyPrototype>())
                {
                    Assert.That(techNamesByPosition.TryGetValue(tech.Position, out var techName), Is.False, $"Tech {tech.ID} has a duplicate position {tech.Position} with {techName}.");
                    techNamesByPosition[tech.Position] = tech.ID;

                    foreach (var recipe in tech.RecipeUnlocks)
                    {
                        Assert.That(protoManager.TryIndex(recipe, out var proto), Is.True, $"Technology {tech.ID} unlocks recipe {recipe} which does not exist.");
                    }

                    foreach (var prereq in tech.TechnologyPrerequisites)
                    {
                        Assert.That(protoManager.TryIndex(prereq, out var proto), Is.True, $"Technology {tech.ID} has {prereq} as a pre-requisite, but {prereq} is not a valid technology.");
                    }
                }
            });
        });
        await server.WaitRunTicks(1);
        await pair.CleanReturnAsync();
    }
}

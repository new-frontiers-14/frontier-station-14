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
                }
            });
        });
        await server.WaitRunTicks(1);
        await pair.CleanReturnAsync();
    }
}

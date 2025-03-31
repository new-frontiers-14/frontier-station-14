using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._NF.Shuttle;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class ServiceFlagsSuffixTests
{
    TestPair _pair;
    SharedShuttleSystem _shuttle;

    [SetUp]
    public async Task Setup()
    {
        _pair = await PoolManager.GetServerClient();
        var server = _pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        _shuttle = entManager.System<SharedShuttleSystem>();
    }

    [Test]
    public void GetServiceFlagsSuffix_None_ReturnsEmptyString()
    {
        var result = _shuttle.GetServiceFlagsSuffix(ServiceFlags.None);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetServiceFlagsSuffix_SingleFlag_ReturnsSingleCharacter()
    {
        var result = _shuttle.GetServiceFlagsSuffix(ServiceFlags.Services);
        Assert.That(result.Length, Is.Positive);
    }

    [Test]
    public void GetServiceFlagsSuffix_MultipleFlagsUniqueChars_ReturnsFirstCharacters()
    {
        // Assemble all enum values into one
        var valueCount = 0;
        var enumValue = ServiceFlags.None;
        foreach (var flagObj in Enum.GetValues(typeof(ServiceFlags)))
        {
            var flag = (ServiceFlags)flagObj;
            if (flag == ServiceFlags.None)
                continue;
            enumValue |= flag;
            valueCount++;
        }
        var result = _shuttle.GetServiceFlagsSuffix(enumValue);

        // Extract the characters between brackets and split by '|'
        var characters = result.Trim('[', ']').Split('|');
        // Check that we have three separate character combination.
        Assert.Multiple(() =>
        {
            Assert.That(characters, Is.Unique);
            Assert.That(characters.Length, Is.EqualTo(valueCount));

            foreach (var flagObj in Enum.GetValues(typeof(ServiceFlags)))
            {
                var flag = (ServiceFlags)flagObj;
                var oneFlagResult = _shuttle.GetServiceFlagsSuffix(flag);
                // Extract the characters between brackets and split by '|'
                var oneFlagCharacters = oneFlagResult.Trim('[', ']').Split('|');
                // Check that we have three separate character combination.
                Assert.That(oneFlagCharacters.Length, Is.EqualTo(1));
                Assert.That(characters.Contains(oneFlagCharacters[0]), Is.True);
            }
        });
    }
}

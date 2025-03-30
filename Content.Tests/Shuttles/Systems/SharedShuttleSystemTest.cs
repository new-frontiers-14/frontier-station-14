using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using NUnit.Framework;

namespace Content.Tests.Shuttles.Systems;

[TestFixture]
public sealed class ServiceFlagsSuffixTests
{
    [Test]
    public void GetServiceFlagsSuffix_None_ReturnsEmptyString()
    {
        var result = SharedShuttleSystem.GetServiceFlagsSuffix(ServiceFlags.None);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetServiceFlagsSuffix_SingleFlag_ReturnsSingleCharacter()
    {
        var result = SharedShuttleSystem.GetServiceFlagsSuffix(ServiceFlags.Medical);
        Assert.That(result, Is.EqualTo("[M]"));
    }

    [Test]
    public void GetServiceFlagsSuffix_MultipleFlagsUniqueChars_ReturnsFirstCharacters()
    {
        var result = SharedShuttleSystem.GetServiceFlagsSuffix(
            ServiceFlags.Medical | ServiceFlags.Kitchen | ServiceFlags.Trade);

        // Extract the characters between brackets and split by '|'
        var characters = result.Trim('[', ']').Split('|');
        // Check that we have exactly the expected characters, regardless of order
        Assert.That(characters, Is.EquivalentTo(["M", "K", "T"]));
    }

    /// <summary>
    /// This test checks that the suffix uses the first two characters for flags that share
    /// first letters with other flags, even if only one of those flags is active.
    /// For example, if only ServiceFlags.Service is set (and not ServiceFlags.Social),
    /// it should still be represented as "SE" instead of "S".
    /// </summary>
    [Test]
    public void GetServiceFlagsSuffix_FlagWithSharedFirstChar_UsesFirstTwoChars()
    {
        // Test with only one of the flags that share first character
        var result = SharedShuttleSystem.GetServiceFlagsSuffix(ServiceFlags.Service);
        Assert.That(result, Is.EqualTo("[SE]"));

        // Test with the other flag that shares the same first character
        result = SharedShuttleSystem.GetServiceFlagsSuffix(ServiceFlags.Social);
        Assert.That(result, Is.EqualTo("[SO]"));
    }

    [Test]
    public void GetServiceFlagsSuffix_MixedFlags_HandlesCorrectly()
    {
        var result = SharedShuttleSystem.GetServiceFlagsSuffix(
            ServiceFlags.Medical | ServiceFlags.Service | ServiceFlags.Social | ServiceFlags.Trade);

        // Extract the characters between brackets and split by '|'
        var characters = result.Trim('[', ']').Split('|');
        // Check that we have exactly the expected characters, regardless of order
        Assert.That(characters, Is.EquivalentTo(["M", "SE", "T", "SO"]));
    }

}

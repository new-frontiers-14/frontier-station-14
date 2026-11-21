using Content.Client._NF.Guidebook;
using NUnit.Framework;

namespace Content.Tests.Client._NF.Guidebook;

[TestFixture]
[TestOf(typeof(GuidebookSearchEntry))]
public sealed class GuidebookSearchEntryTest
{
    [TestCase("preflight", true)]
    [TestCase("PRE-FLIGHT", true)]
    [TestCase("checklist ship", true)]
    [TestCase("ship docking", true)]
    [TestCase("docking navigation", true)]
    [TestCase("ship medicine", false)]
    [TestCase("", true)]
    [TestCase(" -- ", true)]
    public void MatchesTitlesAndAliases(string query, bool expected)
    {
        var entry = new GuidebookSearchEntry("Ship Pre-flight Checklist", new[] { "navigation", "docking" });
        Assert.That(entry.Matches(query), Is.EqualTo(expected));
    }
}

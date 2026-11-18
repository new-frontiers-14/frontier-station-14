using System;
using System.Text.Json;
using Content.Server._UNEON.Subscriptions;
using NUnit.Framework;

namespace Content.Tests.Server._UNEON;

[TestFixture]
public sealed class SubscriptionCacheTest
{
    private static readonly DateTimeOffset Verified = new(2026, 11, 18, 0, 0, 0, TimeSpan.Zero);

    private static UneonSubscriptionEntry Supporter() => new()
    {
        Linked = true,
        Tier = 1,
        Permissions = new[] { "supporter" },
        ConfirmedAt = Verified,
    };

    [Test]
    public void OutageGraceExpiresWithoutExtendingVerification()
    {
        var entry = Supporter();
        Assert.That(entry.Grants("supporter", Verified.AddHours(5), 360), Is.True);
        Assert.That(entry.Grants("supporter", Verified.AddHours(7), 360), Is.False);
        Assert.That(entry.ConfirmedAt, Is.EqualTo(Verified));
    }

    [Test]
    public void PersistentRoundTripDoesNotRenewGrace()
    {
        var restored = JsonSerializer.Deserialize<UneonSubscriptionEntry>(JsonSerializer.Serialize(Supporter()))!;
        Assert.That(restored.Grants("supporter", Verified.AddHours(5), 360), Is.True);
        Assert.That(restored.Grants("supporter", Verified.AddHours(7), 360), Is.False);
    }

    [Test]
    public void ExplicitRevocationBeatsRemainingGrace()
    {
        var entry = Supporter();
        entry.Tier = 0;
        Assert.That(entry.Grants("supporter", Verified.AddMinutes(1), 360), Is.False);
    }

    [Test]
    public void UnlinkedAndUnknownPermissionsDoNotGrantRights()
    {
        var entry = Supporter();
        Assert.That(entry.Grants("admin", Verified, 360), Is.False);
        entry.Linked = false;
        Assert.That(entry.Grants("supporter", Verified, 360), Is.False);
    }

    [Test]
    public void FutureTimestampAndExcessiveGraceCannotExtendRights()
    {
        var entry = Supporter();
        Assert.That(entry.Grants("supporter", Verified.AddMinutes(-1), 360), Is.False);
        Assert.That(entry.Grants("supporter", Verified.AddHours(25), int.MaxValue), Is.False);
    }
}

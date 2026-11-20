using Content.Shared._UNEON.VipUplink;
using NUnit.Framework;

namespace Content.Tests.Server._UNEON;

[TestFixture]
public sealed class VipUplinkAccessTest
{
    private const string Owner = "00000000-0000-0000-0000-000000000001";
    private const string Other = "00000000-0000-0000-0000-000000000002";

    [Test]
    public void CommonMaterialsRequireAnActiveSubscription()
    {
        var product = new VipUplinkProductPrototype { Price = 3000 };
        Assert.That(product.IsAvailable(0, Owner), Is.False);
        for (var tier = 1; tier <= 5; tier++)
            Assert.That(product.IsAvailable(tier, Owner), Is.True);
    }

    [Test]
    public void PersonalSuitRequiresBothOwnerAndBaronTier()
    {
        var suit = new VipUplinkProductPrototype
        {
            Price = 50000,
            MinimumTier = 4,
            AllowedAccounts = new() { Owner }
        };
        Assert.That(suit.IsAvailable(3, Owner), Is.False);
        Assert.That(suit.IsAvailable(4, Owner), Is.True);
        Assert.That(suit.IsAvailable(5, Owner), Is.True);
        Assert.That(suit.IsAvailable(4, Other), Is.False);
        Assert.That(suit.IsAvailable(5, Other), Is.False);
        Assert.That(suit.IsAvailable(0, Owner), Is.False);
    }

    [Test]
    public void InvalidPricesAndTierRequirementsNeverGrantAccess()
    {
        var product = new VipUplinkProductPrototype { Price = 0 };
        Assert.That(product.IsAvailable(5, Owner), Is.False);
        product.Price = -1;
        Assert.That(product.IsAvailable(5, Owner), Is.False);
        product.Price = 1;
        product.MinimumTier = 0;
        Assert.That(product.IsAvailable(5, Owner), Is.False);
    }
}

using System.Collections.Generic;
using Content.Shared._UNEON.HardsuitUpgrade;
using NUnit.Framework;

namespace Content.Tests.Server._UNEON;

[TestFixture]
public sealed class HardsuitUpgradeRulesTest
{
    [TestCase(35, 40, 15, 15, 0, 0)]
    [TestCase(36, 41, 30, 30, 15, 0)]
    [TestCase(40, 45, 30, 30, 15, 0)]
    [TestCase(55, 60, 30, 30, 15, 0)]
    [TestCase(56, 61, 60, 30, 30, 1)]
    [TestCase(60, 65, 60, 30, 30, 1)]
    public void PriceAndDurationUseTheResult(float current, float expected, int seconds, int plasteel, int gold, int crystals)
    {
        var target = HardsuitUpgradeRules.Target(HardsuitUpgradeKind.Blunt, current);
        var cost = HardsuitUpgradeRules.Cost(HardsuitUpgradeKind.Blunt, target);
        Assert.That(target, Is.EqualTo(expected));
        Assert.That(HardsuitUpgradeRules.Seconds(HardsuitUpgradeKind.Blunt, target), Is.EqualTo(seconds));
        Assert.That(cost["Steel"], Is.EqualTo(30));
        Assert.That(cost["Plasteel"], Is.EqualTo(plasteel));
        Assert.That(cost.GetValueOrDefault("Gold"), Is.EqualTo(gold));
        Assert.That(cost.GetValueOrDefault("Bluespace"), Is.EqualTo(crystals));
        Assert.That(cost["Plastic"], Is.EqualTo(target > 60 ? 30 : 10));
        Assert.That(cost.GetValueOrDefault("Uranium"), Is.EqualTo(gold));
        Assert.That(cost.GetValueOrDefault("Plasma"), Is.EqualTo(gold));
    }

    [TestCase(HardsuitUpgradeKind.Blunt, 80)]
    [TestCase(HardsuitUpgradeKind.Slash, 80)]
    [TestCase(HardsuitUpgradeKind.Piercing, 80)]
    [TestCase(HardsuitUpgradeKind.Heat, 100)]
    [TestCase(HardsuitUpgradeKind.Radiation, 100)]
    [TestCase(HardsuitUpgradeKind.Caustic, 95)]
    [TestCase(HardsuitUpgradeKind.Explosion, 70)]
    public void CapsApplyToTheLastStepWithoutDowngradingBetterSuits(HardsuitUpgradeKind kind, float cap)
    {
        Assert.That(HardsuitUpgradeRules.Limit(kind), Is.EqualTo(cap));
        Assert.That(HardsuitUpgradeRules.Target(kind, cap - 2), Is.EqualTo(cap));
        Assert.That(HardsuitUpgradeRules.CanImprove(kind, cap), Is.False);
        Assert.That(HardsuitUpgradeRules.CanImprove(kind, cap + 10), Is.False);
        Assert.That(HardsuitUpgradeRules.Target(kind, cap + 10), Is.EqualTo(cap + 10));
    }

    [TestCase(30, 25)]
    [TestCase(5, 0)]
    [TestCase(2, 0)]
    public void MobilityAlwaysCostsFiveCrystalsAndNeverAddsASpeedBonus(float current, float target)
    {
        Assert.That(HardsuitUpgradeRules.Target(HardsuitUpgradeKind.Speed, current), Is.EqualTo(target));
        Assert.That(HardsuitUpgradeRules.Cost(HardsuitUpgradeKind.Speed, target),
            Is.EquivalentTo(new Dictionary<string, int> { ["Bluespace"] = 5 }));
        Assert.That(HardsuitUpgradeRules.Seconds(HardsuitUpgradeKind.Speed, target), Is.EqualTo(60));
        Assert.That(HardsuitUpgradeRules.CanImprove(HardsuitUpgradeKind.Speed, 0), Is.False);
        Assert.That(HardsuitUpgradeRules.CanImprove(HardsuitUpgradeKind.Speed, -10), Is.False);
    }

    [Test]
    public void FloatingPointArmorCoefficientsDoNotMisclassifyBoundaryRecipes()
    {
        var current = HardsuitUpgradeRules.Protection(0.45f);
        var target = HardsuitUpgradeRules.Target(HardsuitUpgradeKind.Piercing, current);
        Assert.That(current, Is.EqualTo(55));
        Assert.That(target, Is.EqualTo(60));
        Assert.That(HardsuitUpgradeRules.Cost(HardsuitUpgradeKind.Piercing, target).ContainsKey("Bluespace"), Is.False);
        Assert.That(HardsuitUpgradeRules.CanImprove(HardsuitUpgradeKind.Heat, float.NaN), Is.False);
    }
}

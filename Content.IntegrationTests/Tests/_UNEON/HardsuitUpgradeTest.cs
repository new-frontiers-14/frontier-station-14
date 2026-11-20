using System.Linq;
using Content.Server._UNEON.HardsuitUpgrade;
using Content.Server.Armor;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared._UNEON.HardsuitUpgrade;
using Content.Shared.Armor;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._UNEON;

[TestFixture]
public sealed class HardsuitUpgradeTest
{
    [Test]
    public async Task UpgradeChargesOncePausesWithoutPowerAndOnlyChangesTheInsertedSuit()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var system = em.System<HardsuitUpgradeSystem>();
            var materials = em.System<MaterialStorageSystem>();
            var slots = em.System<ItemSlotsSystem>();
            var station = em.SpawnEntity("UneonHardsuitUpgradeStation", map.GridCoords);
            var suit = em.SpawnEntity("ClothingOuterHardsuitEngineering", map.GridCoords);
            var other = em.SpawnEntity("ClothingOuterHardsuitEngineering", map.GridCoords);
            var actor = em.SpawnEntity("MobHuman", map.GridCoords);
            var power = em.GetComponent<ApcPowerReceiverComponent>(station);
            var job = em.GetComponent<HardsuitUpgradeComponent>(station);
            var starting = system.Current(suit, HardsuitUpgradeKind.Blunt);
            var appearance = em.GetComponent<ClothingComponent>(suit).RsiPath;
            Assert.That(slots.TryInsert(station, HardsuitUpgradeRules.Slot, suit, null), Is.True);
            Assert.That(slots.TryInsert(station, HardsuitUpgradeRules.Slot, other, null), Is.False);
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Blunt), Is.False);
            power.Powered = true;
            var steel = em.SpawnEntity("SheetSteel", map.GridCoords);
            Assert.That(materials.TryInsertMaterialEntity(actor, steel, station), Is.True);
            Assert.That(materials.GetMaterialAmount(station, "Steel"), Is.EqualTo(3000));
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Blunt), Is.False);
            Assert.That(materials.GetMaterialAmount(station, "Steel"), Is.EqualTo(3000), "An incomplete recipe must not take a partial payment.");
            materials.TrySetMaterialAmount(station, "Plasteel", 1500);
            materials.TrySetMaterialAmount(station, "Plastic", 1000);
            Assert.That(system.TryStartUpgrade(station, (HardsuitUpgradeKind) 255), Is.False);
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Blunt), Is.True);
            Assert.That(materials.GetMaterialAmount(station, "Steel"), Is.Zero);
            Assert.That(materials.GetMaterialAmount(station, "Plasteel"), Is.Zero);
            Assert.That(materials.GetMaterialAmount(station, "Plastic"), Is.Zero);
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Blunt), Is.False);
            Assert.That(slots.TryEject(station, HardsuitUpgradeRules.Slot, null, out _), Is.False);
            power.Powered = false;
            system.Update(120);
            Assert.That(job.Remaining, Is.EqualTo(15));
            Assert.That(system.Current(suit, HardsuitUpgradeKind.Blunt), Is.EqualTo(starting));
            power.Powered = true;
            system.Update(15);
            Assert.That(job.Job, Is.Null);
            Assert.That(slots.GetItemOrNull(station, HardsuitUpgradeRules.Slot), Is.Null);
            Assert.That(system.Current(suit, HardsuitUpgradeKind.Blunt), Is.EqualTo(starting + 5));
            Assert.That(system.Current(other, HardsuitUpgradeKind.Blunt), Is.EqualTo(starting));
            var fresh = em.SpawnEntity("ClothingOuterHardsuitEngineering", map.GridCoords);
            Assert.That(system.Current(fresh, HardsuitUpgradeKind.Blunt), Is.EqualTo(starting));
            Assert.That(em.GetComponent<ClothingComponent>(suit).RsiPath, Is.EqualTo(appearance));

            // Terminal steps must stop at the cap and leave superior initial armor alone.
            Assert.That(slots.TryInsert(station, HardsuitUpgradeRules.Slot, suit, null), Is.True);
            foreach (var material in new[] { "Steel", "Plasteel", "Plastic", "Gold", "Uranium", "Plasma", "Bluespace" })
                materials.TrySetMaterialAmount(station, material, 10000);
            em.System<ArmorSystem>().SetDamageCoefficient((suit, em.GetComponent<ArmorComponent>(suit)), "Blunt", 0.21f);
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Blunt), Is.True);
            Assert.That(job.Duration, Is.EqualTo(60));
            system.Update(60);
            Assert.That(system.Current(suit, HardsuitUpgradeKind.Blunt), Is.EqualTo(80));
            Assert.That(slots.TryInsert(station, HardsuitUpgradeRules.Slot, suit, null), Is.True);
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Blunt), Is.False);
            em.System<ArmorSystem>().SetDamageCoefficient((suit, em.GetComponent<ArmorComponent>(suit)), "Blunt", 0.1f);
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Blunt), Is.False);
            Assert.That(system.Current(suit, HardsuitUpgradeKind.Blunt), Is.EqualTo(90));

            // Explosion resistance uses the explosion component, not a fictitious damage type.
            em.System<SharedExplosionSystem>().SetDamageCoefficient((suit, em.GetComponent<ExplosionResistanceComponent>(suit)), 0.31f);
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Explosion), Is.True);
            system.Update(60);
            Assert.That(system.Current(suit, HardsuitUpgradeKind.Explosion), Is.EqualTo(70));
            Assert.That(slots.TryInsert(station, HardsuitUpgradeRules.Slot, suit, null), Is.True);
            var speed = em.GetComponent<ClothingSpeedModifierComponent>(suit);
            var walk = speed.WalkModifier;
            var sprint = speed.SprintModifier;
            var crystals = materials.GetMaterialAmount(station, "Bluespace");
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Speed), Is.True);
            Assert.That(materials.GetMaterialAmount(station, "Bluespace"), Is.EqualTo(crystals - 500));
            system.Update(60);
            Assert.That(speed.WalkModifier, Is.EqualTo(walk + 0.05f).Within(0.0001));
            Assert.That(speed.SprintModifier, Is.EqualTo(sprint + 0.05f).Within(0.0001));

            // Deconstruction returns a paid but unfinished recipe without applying its bonus.
            Assert.That(slots.TryInsert(station, HardsuitUpgradeRules.Slot, suit, null), Is.True);
            crystals = materials.GetMaterialAmount(station, "Bluespace");
            Assert.That(system.TryStartUpgrade(station, HardsuitUpgradeKind.Speed), Is.True);
            em.EventBus.RaiseLocalEvent(station, new MachineDeconstructedEvent());
            Assert.That(job.Job, Is.Null);
            Assert.That(materials.GetMaterialAmount(station, "Bluespace"), Is.EqualTo(crystals));
            Assert.That(speed.WalkModifier, Is.EqualTo(walk + 0.05f).Within(0.0001));
            foreach (var entity in new[] { station, suit, other, fresh, actor })
                em.DeleteEntity(entity);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ResearchUnlocksAnActuallyPrintableAndConstructibleMachineBoard()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Server.WaitAssertion(() =>
        {
            var prototypes = pair.Server.ProtoMan;
            var tech = prototypes.Index<TechnologyPrototype>("UneonHardsuitUpgrades");
            Assert.That(tech.TechnologyPrerequisites.Select(p => p.ToString()), Does.Contain("NFImprovedSalvageWeapons"));
            Assert.That(tech.RecipeUnlocks.Select(p => p.ToString()), Does.Contain("UneonHardsuitUpgradeMachineCircuitboard"));
            var recipe = prototypes.Index<LatheRecipePrototype>("UneonHardsuitUpgradeMachineCircuitboard");
            Assert.That(recipe.Result.ToString(), Is.EqualTo("UneonHardsuitUpgradeMachineCircuitboard"));
            Assert.That(prototypes.Index<LatheRecipePackPrototype>("NFEngineeringBoardsCircuitImprinter").Recipes.Select(p => p.ToString()),
                Does.Contain("UneonHardsuitUpgradeMachineCircuitboard"));
            Assert.That(prototypes.Index<LatheRecipePackPrototype>("NFBlueprintsSalvage").Recipes.Select(p => p.ToString()),
                Does.Contain("UneonHardsuitUpgradeMachineCircuitboard"));
        });
        await pair.CleanReturnAsync();
    }
}

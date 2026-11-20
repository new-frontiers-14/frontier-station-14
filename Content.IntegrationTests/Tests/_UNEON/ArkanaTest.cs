using System.Linq;
using Content.Server.Wagging;
using Content.Shared.Body.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._UNEON;

[TestFixture]
public sealed class ArkanaTest
{
    [Test]
    public async Task ArkanaKeepsHumanOrgansAndCanWearStandardEquipment()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var entities = server.EntMan;
            var species = server.ProtoMan.Index<SpeciesPrototype>("Arkana");
            Assert.That(species.RoundStart, Is.True);
            var human = server.ProtoMan.Index<BodyPrototype>("Human");
            var arkana = server.ProtoMan.Index<BodyPrototype>("Arkana");
            foreach (var (slot, humanSlot) in human.Slots)
                Assert.That(arkana.Slots[slot].Organs, Is.EquivalentTo(humanSlot.Organs));

            var inventory = entities.System<InventorySystem>();
            var appearance = entities.System<SharedHumanoidAppearanceSystem>();
            foreach (var sex in new[] { Sex.Male, Sex.Female })
            {
                var mob = entities.SpawnEntity("MobArkana", map.GridCoords);
                appearance.SetSex(mob, sex);
                var humanoid = entities.GetComponent<HumanoidAppearanceComponent>(mob);
                Assert.That(humanoid.Species.ToString(), Is.EqualTo("Arkana"));
                Assert.That(humanoid.MarkingSet.Markings[MarkingCategories.Tail].Single().MarkingId,
                    Is.EqualTo("ArkanaTail"));
                var wagging = entities.System<WaggingSystem>();
                Assert.That(wagging.TryToggleWagging(mob), Is.True);
                Assert.That(humanoid.MarkingSet.Markings[MarkingCategories.Tail].Single().MarkingId,
                    Is.EqualTo("ArkanaTailAnimated"));
                Assert.That(wagging.TryToggleWagging(mob), Is.True);

                var uniform = entities.SpawnEntity("ClothingUniformJumpsuitColorGrey", map.GridCoords);
                var suit = entities.SpawnEntity("ClothingOuterHardsuitSyndie", map.GridCoords);
                var helmet = entities.SpawnEntity("ClothingHeadHelmetHardsuitSyndie", map.GridCoords);
                Assert.That(inventory.TryEquip(mob, uniform, "jumpsuit", silent: true), Is.True);
                Assert.That(inventory.TryEquip(mob, suit, "outerClothing", silent: true), Is.True);
                Assert.That(inventory.TryEquip(mob, helmet, "head", silent: true), Is.True);
                entities.DeleteEntity(mob);
            }
            var dummy = entities.SpawnEntity(species.DollPrototype, map.GridCoords);
            Assert.That(entities.GetComponent<HumanoidAppearanceComponent>(dummy).Species.ToString(), Is.EqualTo("Arkana"));
            entities.DeleteEntity(dummy);
        });
        await pair.CleanReturnAsync();
    }
}

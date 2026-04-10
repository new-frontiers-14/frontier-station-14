#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._NF.Paper;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._NF;

/// <summary>
/// Integration tests for the stapler tool and paper bundle system.
/// </summary>
public sealed class StaplerTest : InteractionTest
{
    /// <summary>
    /// Using a stapler on a paper should load the paper into the stapler's item slot.
    /// </summary>
    [Test]
    public async Task LoadPaperIntoStapler()
    {
        // Spawn a paper as the target
        await SpawnTarget("Paper");

        // Use a stapler on the paper — should load the paper into the stapler
        await InteractUsing("Stapler");

        // Get the stapler entity (currently held in hand)
        var staplerUid = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(staplerUid, Is.Not.Null, "Player should still be holding the stapler");

        // Verify the paper is loaded in the stapler's item slot
        await Server.WaitPost(() =>
        {
            var stapler = SEntMan.GetComponent<StaplerComponent>(staplerUid!.Value);
            var slotsSys = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slotsSys.TryGetSlot(staplerUid!.Value, stapler.SlotId, out var slot), Is.True);
            Assert.That(slot!.Item, Is.Not.Null, "Stapler slot should contain the paper");
        });
    }

    /// <summary>
    /// Using a loaded stapler on a second paper should create a PaperBundle with 2 pages.
    /// </summary>
    [Test]
    public async Task StapleTwoPapers()
    {
        // Spawn Paper B as the target
        await SpawnTarget("Paper");

        // Spawn a stapler and load Paper A into it
        var staplerNet = await PlaceInHands("Stapler");

        await Server.WaitPost(() =>
        {
            var staplerUid = SEntMan.GetEntity(staplerNet);
            var paperA = SEntMan.SpawnAtPosition("Paper", SEntMan.GetCoordinates(PlayerCoords));
            var slotsSys = SEntMan.System<ItemSlotsSystem>();
            var stapler = SEntMan.GetComponent<StaplerComponent>(staplerUid);
            slotsSys.TryGetSlot(staplerUid, stapler.SlotId, out var slot);
            slotsSys.TryInsert(staplerUid, slot!, paperA, SPlayer);
        });
        await RunTicks(5);

        // Use loaded stapler on Paper B
        await Interact();
        await RunTicks(5);

        // Look for a PaperBundle entity in the world
        var entities = await DoEntityLookup();
        EntityUid? bundleUid = null;
        await Server.WaitPost(() =>
        {
            foreach (var uid in entities)
            {
                if (SEntMan.HasComponent<PaperBundleComponent>(uid))
                {
                    bundleUid = uid;
                    break;
                }
            }
        });

        Assert.That(bundleUid, Is.Not.Null, "A PaperBundle entity should have been created");

        // Verify bundle contains exactly 2 papers
        await Server.WaitPost(() =>
        {
            var bundle = SEntMan.GetComponent<PaperBundleComponent>(bundleUid!.Value);
            var containerSys = SEntMan.System<SharedContainerSystem>();
            var container = containerSys.GetContainer(bundleUid!.Value, bundle.ContainerId);
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(2),
                "Bundle should contain exactly 2 papers");
        });
    }

    /// <summary>
    /// Using a loaded stapler on an existing bundle should add the page to it.
    /// </summary>
    [Test]
    public async Task AddPageToExistingBundle()
    {
        // Spawn a PaperBundle and pre-fill it with 2 papers
        await SpawnTarget("PaperBundle");
        var bundleNet = Target!.Value;

        await Server.WaitPost(() =>
        {
            var bundleUid = SEntMan.GetEntity(bundleNet);
            var bundle = SEntMan.GetComponent<PaperBundleComponent>(bundleUid);
            var containerSys = SEntMan.System<SharedContainerSystem>();
            var container = containerSys.GetContainer(bundleUid, bundle.ContainerId);
            var coords = SEntMan.GetComponent<TransformComponent>(bundleUid).Coordinates;
            containerSys.Insert(SEntMan.SpawnEntity("Paper", coords), container);
            containerSys.Insert(SEntMan.SpawnEntity("Paper", coords), container);
        });
        await RunTicks(5);

        // Spawn a stapler and load a new paper into it
        var staplerNet = await PlaceInHands("Stapler");

        await Server.WaitPost(() =>
        {
            var staplerUid = SEntMan.GetEntity(staplerNet);
            var newPaper = SEntMan.SpawnAtPosition("Paper", SEntMan.GetCoordinates(PlayerCoords));
            var slotsSys = SEntMan.System<ItemSlotsSystem>();
            var stapler = SEntMan.GetComponent<StaplerComponent>(staplerUid);
            slotsSys.TryGetSlot(staplerUid, stapler.SlotId, out var slot);
            slotsSys.TryInsert(staplerUid, slot!, newPaper, SPlayer);
        });
        await RunTicks(5);

        // Use stapler on the bundle
        await Interact();
        await RunTicks(5);

        // Verify bundle now has 3 pages
        await Server.WaitPost(() =>
        {
            var bundleUid = SEntMan.GetEntity(bundleNet);
            var bundle = SEntMan.GetComponent<PaperBundleComponent>(bundleUid);
            var containerSys = SEntMan.System<SharedContainerSystem>();
            var container = containerSys.GetContainer(bundleUid, bundle.ContainerId);
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(3),
                "Bundle should contain 3 papers after adding one more");
        });
    }

    /// <summary>
    /// A bundle at max capacity should reject additional pages. The paper should remain in the stapler.
    /// </summary>
    [Test]
    public async Task MaxPagesEnforced()
    {
        // Spawn a PaperBundle and fill it to max capacity
        await SpawnTarget("PaperBundle");
        var bundleNet = Target!.Value;
        var maxPages = 0;

        await Server.WaitPost(() =>
        {
            var bundleUid = SEntMan.GetEntity(bundleNet);
            var bundle = SEntMan.GetComponent<PaperBundleComponent>(bundleUid);
            maxPages = bundle.MaxPages;
            var containerSys = SEntMan.System<SharedContainerSystem>();
            var container = containerSys.GetContainer(bundleUid, bundle.ContainerId);
            var coords = SEntMan.GetComponent<TransformComponent>(bundleUid).Coordinates;
            for (var i = 0; i < maxPages; i++)
            {
                containerSys.Insert(SEntMan.SpawnEntity("Paper", coords), container);
            }
        });
        await RunTicks(5);

        // Spawn a stapler and load a paper into it
        var staplerNet = await PlaceInHands("Stapler");

        await Server.WaitPost(() =>
        {
            var staplerUid = SEntMan.GetEntity(staplerNet);
            var newPaper = SEntMan.SpawnAtPosition("Paper", SEntMan.GetCoordinates(PlayerCoords));
            var slotsSys = SEntMan.System<ItemSlotsSystem>();
            var stapler = SEntMan.GetComponent<StaplerComponent>(staplerUid);
            slotsSys.TryGetSlot(staplerUid, stapler.SlotId, out var slot);
            slotsSys.TryInsert(staplerUid, slot!, newPaper, SPlayer);
        });
        await RunTicks(5);

        // Try to add to full bundle — should be rejected
        await Interact();
        await RunTicks(5);

        // Verify bundle still has max pages
        await Server.WaitPost(() =>
        {
            var bundleUid = SEntMan.GetEntity(bundleNet);
            var bundle = SEntMan.GetComponent<PaperBundleComponent>(bundleUid);
            var containerSys = SEntMan.System<SharedContainerSystem>();
            var container = containerSys.GetContainer(bundleUid, bundle.ContainerId);
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(maxPages),
                "Bundle should still be at max capacity");
        });

        // Verify the paper is still in the stapler slot
        await Server.WaitPost(() =>
        {
            var staplerUid = SEntMan.GetEntity(staplerNet);
            var stapler = SEntMan.GetComponent<StaplerComponent>(staplerUid);
            var slotsSys = SEntMan.System<ItemSlotsSystem>();
            slotsSys.TryGetSlot(staplerUid, stapler.SlotId, out var slot);
            Assert.That(slot!.Item, Is.Not.Null,
                "Paper should still be in stapler since bundle was full");
        });
    }

    /// <summary>
    /// Activating a stapler with a loaded paper should eject the paper.
    /// </summary>
    [Test]
    public async Task EjectPaperFromStapler()
    {
        // Spawn paper as target, use stapler on it to load
        await SpawnTarget("Paper");
        await InteractUsing("Stapler");

        // Get stapler entity
        var staplerUid = HandSys.GetActiveItem((SPlayer, Hands));
        Assert.That(staplerUid, Is.Not.Null);

        // Verify paper is loaded
        await Server.WaitPost(() =>
        {
            var stapler = SEntMan.GetComponent<StaplerComponent>(staplerUid!.Value);
            var slotsSys = SEntMan.System<ItemSlotsSystem>();
            slotsSys.TryGetSlot(staplerUid!.Value, stapler.SlotId, out var slot);
            Assert.That(slot!.Item, Is.Not.Null, "Paper should be loaded in stapler");
        });

        // Activate the stapler to eject
        await Activate(SEntMan.GetNetEntity(staplerUid!.Value));
        await RunTicks(5);

        // Verify slot is now empty
        await Server.WaitPost(() =>
        {
            var stapler = SEntMan.GetComponent<StaplerComponent>(staplerUid!.Value);
            var slotsSys = SEntMan.System<ItemSlotsSystem>();
            slotsSys.TryGetSlot(staplerUid!.Value, stapler.SlotId, out var slot);
            Assert.That(slot!.Item, Is.Null, "Stapler slot should be empty after ejection");
        });
    }

    /// <summary>
    /// Using a pen on a bundle should open write mode. Writing via BUI should only affect the last page.
    /// </summary>
    [Test]
    public async Task WriteToBundleLastPage()
    {
        // Spawn a PaperBundle and fill it with 2 papers
        await SpawnTarget("PaperBundle");
        var bundleNet = Target!.Value;

        EntityUid firstPage = default;
        EntityUid lastPage = default;

        await Server.WaitPost(() =>
        {
            var bundleUid = SEntMan.GetEntity(bundleNet);
            var bundle = SEntMan.GetComponent<PaperBundleComponent>(bundleUid);
            var containerSys = SEntMan.System<SharedContainerSystem>();
            var container = containerSys.GetContainer(bundleUid, bundle.ContainerId);
            var coords = SEntMan.GetComponent<TransformComponent>(bundleUid).Coordinates;
            firstPage = SEntMan.SpawnEntity("Paper", coords);
            lastPage = SEntMan.SpawnEntity("Paper", coords);
            containerSys.Insert(firstPage, container);
            containerSys.Insert(lastPage, container);
        });
        await RunTicks(5);

        // Use pen on bundle to open write mode
        await InteractUsing("Pen");
        await RunTicks(10);

        // Send a write message to the last page via BUI
        var lastPageNet = SEntMan.GetNetEntity(lastPage);
        await SendBui(
            PaperBundleComponent.PaperBundleUiKey.Key,
            new PaperBundleComponent.PaperBundleInputTextMessage(lastPageNet, "Hello World"));
        await RunTicks(10);

        // Verify last page has the written content
        await Server.WaitPost(() =>
        {
            var lastPaper = SEntMan.GetComponent<PaperComponent>(lastPage);
            Assert.That(lastPaper.Content, Is.EqualTo("Hello World"),
                "Last page should have the written content");

            // Verify first page is still blank
            var firstPaper = SEntMan.GetComponent<PaperComponent>(firstPage);
            Assert.That(firstPaper.Content, Is.EqualTo(""),
                "First page should still be blank");
        });
    }
}

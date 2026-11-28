using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Guidebook.Controls;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Shared.Guidebook;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._NF;

[TestFixture]
[TestOf(typeof(GuidebookWindow))]
public sealed class GuidebookNavigationTest
{
    private static readonly ProtoId<GuideEntryPrototype> ShipGuide = "ShipyardAdder";
    private static readonly ProtoId<GuideEntryPrototype> PilotingGuide = "Piloting";

    [Test]
    public async Task ContextualHelpKeepsFullContentsAndSearch()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        await pair.Client.WaitAssertion(() =>
        {
            var ui = pair.Client.ResolveDependency<IUserInterfaceManager>();
            var controller = ui.GetUIController<GuidebookUIController>();
            // Headless clients do not enter the gameplay UI automatically.
            var state = new GameplayState();
            controller.OnStateEntered(state);
            try
            {
                // Use the controller overload that receives contextual help, including shipyard manuals.
                controller.OpenGuidebook(new List<ProtoId<GuideEntryPrototype>> { ShipGuide });
                var window = ui.WindowRoot.Children.OfType<GuidebookWindow>().Single();
                Assert.That(window.LastEntry, Is.EqualTo(ShipGuide));
                Assert.That(window.Tree.Items.Any(item => item.Metadata is GuideEntry { Id: "Engineering" }), Is.True);

                window.Tree.SetAllExpanded(false);
                var frontier = window.Tree.Items.Single(item => item.Metadata is GuideEntry { Id: "NF14" });
                var search = window.FindControl<LineEdit>("CategorySearchBar");
                search.SetText("docking navigation", invokeEvent: true);
                Assert.That(frontier.Expanded, Is.True);
                Assert.That(window.Tree.Items.Single(item =>
                    item.Metadata is GuideEntry { Id: "Piloting" }).Visible, Is.True);
                Assert.That(window.Tree.Items.Single(item =>
                    item.Metadata is GuideEntry { Id: "Engineering" }).Visible, Is.False);

                search.SetText("", invokeEvent: true);
                Assert.That(frontier.Expanded, Is.False, "Clearing search must restore the previous expansion state.");
                search.SetText("no-such-guidebook-topic", invokeEvent: true);
                Assert.That(window.FindControl<Label>("CategoryNoResultsLabel").Visible, Is.True);

                // Following a link clears the query and reveals the target's parent categories.
                window.HandleClick(PilotingGuide);
                Assert.That(search.Text, Is.Empty);
                Assert.That(frontier.Expanded, Is.True);
                Assert.That(window.LastEntry, Is.EqualTo(PilotingGuide));
                Assert.That(window.FindControl<Button>("BackButton").Disabled, Is.False);
            }
            finally
            {
                controller.OnStateExited(state);
            }
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task HistoryAndSidebarButtons()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        GuidebookWindow window = null;
        await client.WaitAssertion(() =>
        {
            var prototypes = client.ResolveDependency<IPrototypeManager>();
            window = new GuidebookWindow();
            window.UpdateGuides(new Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry>
            {
                [ShipGuide] = prototypes.Index(ShipGuide),
                [PilotingGuide] = prototypes.Index(PilotingGuide),
            }, selected: ShipGuide);
            window.Open();
            window.HandleClick(PilotingGuide);
        });

        await Click("BackButton");
        await client.WaitAssertion(() => Assert.That(window.LastEntry, Is.EqualTo(ShipGuide)));
        await Click("ForwardButton");
        await client.WaitAssertion(() => Assert.That(window.LastEntry, Is.EqualTo(PilotingGuide)));
        await Click("ContentsButton");
        await client.WaitAssertion(() =>
            Assert.That(window.FindControl<BoxContainer>("TreeBox").Visible, Is.False));
        await Click("ContentsButton");
        await client.WaitAssertion(() =>
        {
            Assert.That(window.FindControl<BoxContainer>("TreeBox").Visible, Is.True);
            window.Close();
        });
        await pair.CleanReturnAsync();

        async Task Click(string name)
        {
            BaseButton button = null;
            ScreenCoordinates pointer = default;
            await client.WaitPost(() =>
            {
                button = window.FindControl<Button>(name);
                button.Measure(new Vector2(150, 40));
                button.Arrange(new UIBox2(0, 0, 150, 40));
                pointer = new ScreenCoordinates(button.GlobalPixelPosition + Vector2.One, default);
            });
            await client.DoGuiEvent(button, new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick,
                BoundKeyState.Down, pointer, true, Vector2.One, Vector2.One));
            await client.DoGuiEvent(button, new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick,
                BoundKeyState.Up, pointer, true, Vector2.One, Vector2.One));
        }
    }

    [Test]
    public async Task LinksCanLeaveAndReturnToFocusedContents()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        await pair.Client.WaitAssertion(() =>
        {
            var prototypes = pair.Client.ResolveDependency<IPrototypeManager>();
            var window = new GuidebookWindow();
            window.UpdateGuides(new Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry>
            {
                [ShipGuide] = prototypes.Index(ShipGuide),
            });

            window.HandleClick(PilotingGuide);
            Assert.That(window.LastEntry, Is.EqualTo(PilotingGuide));

            // Focused contents stay usable after following a link to another page.
            window.HandleClick(ShipGuide);
            Assert.That(window.LastEntry, Is.EqualTo(ShipGuide));

            window.UpdateGuides(new Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry>());
            Assert.That(window.Tree.Items, Is.Empty);
            Assert.That(window.FindControl<Button>("BackButton").Disabled, Is.True);
        });
        await pair.CleanReturnAsync();
    }
}

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Shared.Paper.PaperComponent;
using static Content.Shared._NF.Paper.PaperBundleComponent;

namespace Content.Shared._NF.Paper;

public sealed class PaperBundleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StaplerSystem _stapler = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> WriteTag = "Write";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperBundleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PaperBundleComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PaperBundleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaperBundleComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PaperBundleComponent, PaperBundleInputTextMessage>(OnInputTextMessage);
        SubscribeLocalEvent<PaperBundleComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
    }

    private void OnComponentInit(Entity<PaperBundleComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnContainerInserted(Entity<PaperBundleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        UpdateBundleAppearance(ent);
    }

    private void OnExamined(Entity<PaperBundleComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var bundleContainer = _container.GetContainer(ent, ent.Comp.ContainerId);
        var count = bundleContainer.ContainedEntities.Count;

        using (args.PushGroup(nameof(PaperBundleComponent)))
        {
            args.PushMarkup(Loc.GetString("paper-bundle-examine",
                ("count", count)));
        }
    }

    /// <summary>
    /// Handles pen (write), stamp, and stapler interactions on the bundle.
    /// If the bundle UI is open for the user, stamp/write targets the currently viewed page
    /// (client sends a targeted message). If the UI is not open, stamp/write targets the last page.
    /// </summary>
    private void OnInteractUsing(Entity<PaperBundleComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Handle stapler interaction -- add loaded paper to bundle
        if (HasComp<StaplerComponent>(args.Used))
        {
            args.Handled = _stapler.TryAddToBundle(target, args.User, args.Used);
            return;
        }

        // Handle pen/write interaction -- open UI in write mode on last page
        if (_tag.HasTag(args.Used, WriteTag))
        {
            // Only allow players to write
            if (!HasComp<ActorComponent>(args.User))
            {
                args.Handled = true;
                return;
            }

            args.Handled = true;

            // Populate BUI state with last page in write mode, then open UI
            UpdateBundleUiState(target, writeMode: true);
            _ui.OpenUi(target.Owner, PaperBundleUiKey.Key, args.User);
            return;
        }

        // Handle stamp interaction -- always stamps the last page
        if (TryComp<StampComponent>(args.Used, out var stampComp))
        {
            args.Handled = true;

            var bundleContainer = _container.GetContainer(target, target.Comp.ContainerId);
            if (bundleContainer.ContainedEntities.Count == 0)
                return;

            var lastPage = bundleContainer.ContainedEntities[^1];
            if (!TryComp<PaperComponent>(lastPage, out var paper))
                return;

            _paperSystem.TryStamp((lastPage, paper), new StampDisplayInfo
            {
                StampedName = stampComp.StampedName,
                StampedColor = stampComp.StampedColor
            }, stampComp.StampState);

            _popup.PopupClient(Loc.GetString("paper-component-action-stamp-paper-self",
                ("target", target.Owner),
                ("stamp", args.Used)), args.User, args.User);

            _audio.PlayPredicted(stampComp.Sound, target, args.User);

            // Refresh UI state so stamp is visible
            UpdateBundleUiState(target);
            UpdateBundleAppearance(target);
        }
    }

    private void BeforeUIOpen(Entity<PaperBundleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateBundleUiState(ent);
    }

    private void OnInputTextMessage(Entity<PaperBundleComponent> ent, ref PaperBundleInputTextMessage args)
    {
        var pageUid = GetEntity(args.PageEntity);

        // Validate that the page entity is actually inside this bundle
        var bundleContainer = _container.GetContainer(ent, ent.Comp.ContainerId);
        if (!bundleContainer.Contains(pageUid))
            return;

        if (!TryComp<PaperComponent>(pageUid, out var paper))
            return;

        if (args.Text.Length > paper.ContentSize)
            return;

        _paperSystem.SetContent((pageUid, paper), args.Text);

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} has written on {ToPrettyString(pageUid):entity} (in bundle {ToPrettyString(ent):bundle}) the following text: {args.Text}");

        // Refresh the BUI state so changes are visible
        UpdateBundleUiState(ent);
        UpdateBundleAppearance(ent);
    }

    /// <summary>
    /// Builds and sets the BUI state with all page data from the bundle container.
    /// </summary>
    private void UpdateBundleUiState(Entity<PaperBundleComponent> ent, bool writeMode = false)
    {
        var bundleContainer = _container.GetContainer(ent, ent.Comp.ContainerId);
        var pages = new List<BundlePageData>();

        for (var i = 0; i < bundleContainer.ContainedEntities.Count; i++)
        {
            var paperUid = bundleContainer.ContainedEntities[i];
            if (!TryComp<PaperComponent>(paperUid, out var paper))
                continue;

            // If write mode requested, set the last page to Write mode
            var mode = writeMode && i == bundleContainer.ContainedEntities.Count - 1
                ? PaperAction.Write
                : PaperAction.Read;

            pages.Add(new BundlePageData(
                GetNetEntity(paperUid),
                paper.Content,
                paper.StampedBy,
                mode,
                paper.ContentSize));
        }

        _ui.SetUiState(ent.Owner, PaperBundleUiKey.Key, new PaperBundleBoundUserInterfaceState(pages));
    }

    /// <summary>
    /// Checks all pages in the bundle and sets appearance data indicating
    /// whether any page has written content or stamps.
    /// </summary>
    private void UpdateBundleAppearance(Entity<PaperBundleComponent> ent)
    {
        var bundleContainer = _container.GetContainer(ent, ent.Comp.ContainerId);
        var hasContent = false;

        foreach (var paperUid in bundleContainer.ContainedEntities)
        {
            if (!TryComp<PaperComponent>(paperUid, out var paper))
                continue;

            if (!string.IsNullOrEmpty(paper.Content) || paper.StampedBy.Count > 0)
            {
                hasContent = true;
                break;
            }
        }

        _appearance.SetData(ent, PaperBundleVisuals.HasContent, hasContent);
    }
}

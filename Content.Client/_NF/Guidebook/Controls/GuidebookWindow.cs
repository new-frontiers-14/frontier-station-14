using System.Linq;
using Content.Client._NF.Guidebook;
using Content.Client.UserInterface.Controls.FancyTree;
using Content.Client.UserInterface.Systems.Info;
using Content.Shared.Guidebook;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook.Controls; // Extends the upstream partial class in its original namespace.

// Frontier: extends the upstream partial in its original namespace.
// Search and colors adapted from Starlight #6822 (ScarletLightweaver).
// Sidebar behavior adapted from Starlight #6895 (Sparlight).
public sealed partial class GuidebookWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILocalizationManager _localization = default!;

    public event Action? OnBrowseAllGuides;

    private readonly Dictionary<TreeItem, GuidebookSearchEntry> _searchEntries = new();
    private readonly Dictionary<TreeItem, bool> _preSearchExpanded = new();
    private readonly Dictionary<TreeItem, Color> _entryColors = new();
    private readonly List<ProtoId<GuideEntryPrototype>> _history = new();
    private int _historyIndex = -1;
    private bool _navigatingHistory;
    private bool _sidebarCollapsed;
    private bool _hasSidebar;
    private float? _sidebarSplit;

    private void InitializeNavigation()
    {
        CategorySearchBar.OnTextChanged += _ => HandleCategorySearch();
        BackButton.OnPressed += _ => NavigateHistory(-1);
        ForwardButton.OnPressed += _ => NavigateHistory(1);
        BrowseAllButton.OnPressed += _ => OnBrowseAllGuides?.Invoke();
        ContentsButton.OnPressed += _ =>
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            ApplySidebarCollapsed();
        };
        HomeButton.OnPressed += _ =>
            HandleClick(UserInterfaceManager.GetUIController<InfoUIController>().GetCoreRuleEntry().Id);

        Split.OnSplitResizeFinished += () => _sidebarSplit = Split.SplitCenter;
    }

    private void NavigateToEntry(GuideEntry entry)
    {
        if (!_navigatingHistory && (_historyIndex < 0 || _history[_historyIndex] != entry.Id))
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            _history.Add(entry.Id);
            _historyIndex = _history.Count - 1;
        }

        ShowGuide(entry);
        UpdateNavigationButtons();
    }

    private void NavigateHistory(int offset)
    {
        var next = _historyIndex + offset;
        if (next < 0 || next >= _history.Count)
            return;

        _historyIndex = next;
        _navigatingHistory = true;
        try
        {
            HandleClick(_history[next]);
        }
        finally
        {
            _navigatingHistory = false;
        }
        UpdateNavigationButtons();
    }

    private void ResetNavigationHistory()
    {
        _history.Clear();
        _historyIndex = -1;
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        BackButton.Disabled = _historyIndex <= 0;
        ForwardButton.Disabled = _historyIndex >= _history.Count - 1;
    }

    private void SetSidebarAvailable(bool available)
    {
        _hasSidebar = available;
        ContentsButton.Visible = available;
        ApplySidebarCollapsed();
    }

    private void ApplySidebarCollapsed()
    {
        TreeBox.Visible = _hasSidebar && !_sidebarCollapsed;
        Split.ResizeMode = TreeBox.Visible
            ? SplitContainer.SplitResizeMode.RespectChildrenMinSize
            : SplitContainer.SplitResizeMode.NotResizable;

        if (!TreeBox.Visible)
            Split.State = SplitContainer.SplitState.Auto;
        else if (_sidebarSplit is { } split)
            Split.SplitCenter = split;

        ContentsButton.Text = Loc.GetString(_sidebarCollapsed
            ? "nf-guidebook-show-contents"
            : "nf-guidebook-hide-contents");
    }

    private void ClearCategorySearch()
    {
        CategorySearchBar.SetText("", invokeEvent: true);
    }

    private void ClearGuidebookSearchIndex()
    {
        _searchEntries.Clear();
        _preSearchExpanded.Clear();
        _entryColors.Clear();
    }

    private void IndexGuidebookEntry(TreeItem item, TreeItem? parent, GuideEntry entry)
    {
        _searchEntries[item] = GuidebookSearchEntry.FromEntry(entry, _localization);

        var color = entry.Color;
        if (color == null && entry.Department is { } department &&
            _prototypeManager.TryIndex(department, out var prototype))
            color = prototype.Color;
        if (color == null && parent != null && _entryColors.TryGetValue(parent, out var parentColor))
            color = parentColor;
        if (color is not { } entryColor)
            return;

        _entryColors[item] = entryColor;
        var textColor = entryColor;
        if (Color.ToHsv(textColor).Z < 0.6f)
        {
            var hsl = Color.ToHsl(textColor);
            hsl.Z = 0.65f;
            textColor = Color.FromHsl(hsl);
        }
        item.Label.FontColorOverride = textColor;

        var marker = new PanelContainer
        {
            SetWidth = 5,
            Margin = new Thickness(0, 3, 3, 3),
            PanelOverride = new StyleBoxFlat(entryColor),
        };
        item.Label.Parent!.AddChild(marker);
        marker.SetPositionInParent(item.Label.GetPositionInParent());
    }

    private void HandleCategorySearch()
    {
        var query = CategorySearchBar.Text.Trim();
        if (query.Length == 0)
        {
            CategoryNoResultsLabel.Visible = false;
            foreach (var item in Tree.Items)
            {
                item.Visible = true;
                if (_preSearchExpanded.TryGetValue(item, out var expanded))
                    item.SetExpanded(expanded);
                item.UpdateIcon();
            }
            _preSearchExpanded.Clear();
        }
        else
        {
            if (_preSearchExpanded.Count == 0)
            {
                foreach (var item in Tree.Items)
                    _preSearchExpanded[item] = item.Expanded;
            }

            var anyVisible = false;
            foreach (var child in Tree.Body.Children)
            {
                if (child is TreeItem item)
                    anyVisible |= FilterCategoryTree(item, query);
            }
            CategoryNoResultsLabel.Visible = !anyVisible;
        }
        Tree.QueueRowStyleUpdate();
    }

    /// <summary>
    /// Keep matches, their ancestors, and all descendants of a matching category visible.
    /// </summary>
    private bool FilterCategoryTree(TreeItem item, string query, bool parentMatches = false)
    {
        var matches = parentMatches || _searchEntries[item].Matches(query);
        var childMatches = false;
        foreach (var child in item.Body.Children)
        {
            if (child is TreeItem childItem)
                childMatches |= FilterCategoryTree(childItem, query, matches);
        }

        item.Visible = matches || childMatches;
        if (childMatches)
            item.SetExpanded(true);
        item.UpdateIcon();
        return item.Visible;
    }
}

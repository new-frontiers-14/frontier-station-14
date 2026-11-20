using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._UNEON.HardsuitUpgrade;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._UNEON.HardsuitUpgrade;

public sealed class HardsuitUpgradeWindow : FancyWindow
{
    private readonly SpriteView _preview = new()
    {
        SetSize = new(64, 64),
        Scale = new(2, 2),
        OverrideDirection = Direction.South
    };
    private readonly Label _suit = new() { StyleClasses = { StyleBase.StyleClassLabelHeading } };
    private readonly Label _status = new();
    private readonly Label _materials = new() { StyleClasses = { StyleBase.StyleClassLabelSubText } };
    private readonly Label _silo = new() { StyleClasses = { StyleBase.StyleClassLabelSubText } };
    private readonly ProgressBar _progress = new() { MinValue = 0, MaxValue = 1, MinHeight = 16 };
    private readonly Button _eject = new() { Text = Loc.GetString("uneon-hardsuit-eject") };
    private readonly Button _ejectMaterials = new() { Text = Loc.GetString("uneon-hardsuit-eject-materials") };
    private readonly Dictionary<HardsuitUpgradeKind, (Label Stat, Label Cost, Button Buy, PanelContainer Row)> _rows = new();
    public event Action<HardsuitUpgradeKind>? Upgrade;
    public event Action? Eject;
    public event Action? EjectMaterials;

    public HardsuitUpgradeWindow()
    {
        Title = Loc.GetString("uneon-hardsuit-title");
        MinWidth = 660;
        MinHeight = 540;
        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new(12), SeparationOverride = 8 };
        ContentsContainer.AddChild(root);
        var showcase = new BoxContainer { SeparationOverride = 12 };
        var previewPanel = new PanelContainer { StyleClasses = { "PdaBackground" } };
        previewPanel.AddChild(_preview);
        showcase.AddChild(previewPanel);
        var details = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
        details.AddChild(_suit);
        details.AddChild(_status);
        details.AddChild(_progress);
        showcase.AddChild(details);
        root.AddChild(showcase);
        root.AddChild(new Label
        {
            Text = Loc.GetString("uneon-hardsuit-stored"),
            StyleClasses = { StyleBase.StyleClassLabelHeading }
        });
        root.AddChild(_silo);
        root.AddChild(_materials);
        var scroll = new ScrollContainer { VerticalExpand = true, MinHeight = 350 };
        var rows = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
        scroll.AddChild(rows);
        root.AddChild(scroll);
        foreach (var kind in Enum.GetValues<HardsuitUpgradeKind>())
        {
            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new(8, 5) };
            var header = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 12 };
            var stat = new Label { HorizontalExpand = true };
            var cost = new Label { StyleClasses = { StyleBase.StyleClassLabelSubText } };
            var buy = new Button { MinWidth = 160, MinHeight = 30, StyleClasses = { StyleBase.ButtonSquare } };
            buy.OnPressed += _ => { buy.Disabled = true; Upgrade?.Invoke(kind); };
            header.AddChild(stat);
            header.AddChild(buy);
            row.AddChild(header);
            row.AddChild(cost);
            var card = new PanelContainer { StyleClasses = { "PdaBackground" }, Margin = new(0, 2) };
            card.AddChild(row);
            rows.AddChild(card);
            _rows.Add(kind, (stat, cost, buy, card));
        }
        var buttons = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };
        _eject.AddStyleClass(StyleBase.ButtonSquare);
        _ejectMaterials.AddStyleClass(StyleBase.ButtonSquare);
        _eject.MinHeight = _ejectMaterials.MinHeight = 32;
        _eject.HorizontalExpand = _ejectMaterials.HorizontalExpand = true;
        buttons.AddChild(_eject);
        buttons.AddChild(_ejectMaterials);
        root.AddChild(buttons);
        _eject.OnPressed += _ => Eject?.Invoke();
        _ejectMaterials.OnPressed += _ => EjectMaterials?.Invoke();
    }

    private static string Material(string id) => Loc.GetString("uneon-hardsuit-material-" + id.ToLowerInvariant());

    private static string Quantities(Dictionary<string, int> values)
    {
        // Keep long recipes within the window width without rebuilding controls on progress updates.
        var parts = values.Select(p => $"{Material(p.Key)}: {p.Value}").ToArray();
        return parts.Length <= 4 ? string.Join("; ", parts)
            : string.Join("; ", parts.Take(4)) + "\n" + string.Join("; ", parts.Skip(4));
    }

    public void UpdateState(HardsuitUpgradeState state)
    {
        _preview.SetEntity(state.Suit ?? NetEntity.Invalid);
        _suit.Text = state.SuitName ?? Loc.GetString("uneon-hardsuit-insert");
        _status.Text = state.Busy
            ? Loc.GetString(state.Powered ? "uneon-hardsuit-working" : "uneon-hardsuit-paused",
                ("seconds", (int) MathF.Ceiling(state.Remaining)))
            : Loc.GetString(state.Powered ? "uneon-hardsuit-ready" : "uneon-hardsuit-no-power");
        _progress.Value = state.Duration > 0 ? 1 - state.Remaining / state.Duration : 0;
        _materials.Text = state.Materials.Count > 0 ? Quantities(state.Materials) : Loc.GetString("uneon-hardsuit-empty");
        _silo.Text = Loc.GetString(state.SiloAvailable ? "uneon-hardsuit-silo-available" :
            state.SiloLinked ? "uneon-hardsuit-silo-unavailable" : "uneon-hardsuit-silo-unlinked");
        _eject.Disabled = state.Busy || state.SuitName == null;
        _ejectMaterials.Disabled = state.Busy || !state.HasLocalMaterials;
        foreach (var row in _rows.Values)
            row.Row.Visible = false;
        foreach (var option in state.Options)
        {
            var row = _rows[option.Kind];
            row.Row.Visible = true;
            var name = Loc.GetString("uneon-hardsuit-stat-" + option.Kind.ToString().ToLowerInvariant());
            row.Stat.Text = Loc.GetString("uneon-hardsuit-stat", ("name", name), ("current", option.Current),
                ("target", option.Target), ("limit", HardsuitUpgradeRules.Limit(option.Kind)));
            row.Cost.Text = option.Available ? Quantities(HardsuitUpgradeRules.Cost(option.Kind, option.Target)) : "";
            row.Buy.Text = option.Available
                ? Loc.GetString("uneon-hardsuit-upgrade", ("seconds", HardsuitUpgradeRules.Seconds(option.Kind, option.Target)))
                : Loc.GetString("uneon-hardsuit-limit");
            row.Buy.Disabled = state.Busy || !state.Powered || !option.Available || !option.Affordable;
        }
    }
}

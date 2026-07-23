using Content.Shared.Decals;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Linq; // Frontier

namespace Content.Client.SprayPainter.UI;

/// <summary>
/// A BUI for a spray painter. Allows selecting pipe colours, decals, and paintable object types sorted by category.
/// </summary>
public sealed class SprayPainterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SprayPainterWindow? _window;

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            _window = this.CreateWindow<SprayPainterWindow>();

            _window.OnSpritePicked += OnSpritePicked;
            _window.OnSetPipeColor += OnSetPipeColor;
            _window.OnTabChanged += OnTabChanged;
            _window.OnDecalChanged += OnDecalChanged;
            _window.OnDecalColorChanged += OnDecalColorChanged;
            _window.OnDecalAngleChanged += OnDecalAngleChanged;
            _window.OnDecalSnapChanged += OnDecalSnapChanged;
        }

        var sprayPainter = EntMan.System<SprayPainterSystem>();
        // Frontier start - Specify available painting styles/decals on painter comp
        if (!EntMan.TryGetComponent<SprayPainterComponent>(Owner, out var sprayPainterComp))
            return;

        var availableDecals = new List<SprayPainterDecalEntry>();
        // If a hidden decal tag list isn't given, show all.
        if (!sprayPainterComp.HiddenDecals.Any())
            availableDecals = sprayPainter.Decals;
        // If a hidden decal tag list is given, filter out all decals with a tag from the list.
        else
            foreach (var decal in sprayPainter.Decals)
            {
                if (!sprayPainterComp.HiddenDecals.Intersect(decal.Tags).Any())
                    availableDecals.Add(decal);
            }

        //var availableDecals = sprayPainter.Decals.Where(x => sprayPainterComp.PaintableDecals.Intersect(x.Tags).Count() == x.Tags.Count).ToList();
        var availableStylesGroup = new Dictionary<string, Dictionary<string, EntProtoId>>();
        // Iterate each group
        foreach (var styles in sprayPainter.PaintableStylesByGroup)
        {
            var availableStyles = new Dictionary<string, EntProtoId>();
            // Iterate each name/id pair in each group
            foreach (var style in styles.Value)
            {
                if (sprayPainterComp.HiddenStyles.Contains(style.Key))
                    continue;
                availableStyles.Add(style.Key, style.Value);
            }
            availableStylesGroup.Add(styles.Key, availableStyles);
        }

        //var availableStyles = sprayPainter.PaintableStylesByGroup.Where(x => !sprayPainterComp.HiddenStyles.Contains(x.Value))
        _window.PopulateCategories(availableStylesGroup, sprayPainter.PaintableGroupsByCategory, availableDecals);
        Update();

        _window.SetSelectedTab(sprayPainterComp.SelectedTab);
        // Frontier end
    }

    public override void Update()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out SprayPainterComponent? sprayPainter))
            return;

        _window.PopulateColors(sprayPainter.ColorPalette);
        if (sprayPainter.PickedColor != null)
            _window.SelectColor(sprayPainter.PickedColor);
        _window.SetSelectedStyles(sprayPainter.StylesByGroup);
        _window.SetSelectedDecal(sprayPainter.SelectedDecal);
        _window.SetDecalAngle(sprayPainter.SelectedDecalAngle);
        _window.SetDecalColor(sprayPainter.SelectedDecalColor);
        _window.SetDecalSnap(sprayPainter.SnapDecals);
    }

    private void OnDecalSnapChanged(bool snap)
    {
        SendPredictedMessage(new SprayPainterSetDecalSnapMessage(snap));
    }

    private void OnDecalAngleChanged(int angle)
    {
        SendPredictedMessage(new SprayPainterSetDecalAngleMessage(angle));
    }

    private void OnDecalColorChanged(Color? color)
    {
        SendPredictedMessage(new SprayPainterSetDecalColorMessage(color));
    }

    private void OnDecalChanged(ProtoId<DecalPrototype> protoId)
    {
        SendPredictedMessage(new SprayPainterSetDecalMessage(protoId));
    }

    private void OnTabChanged(int index, bool isSelectedTabWithDecals)
    {
        SendPredictedMessage(new SprayPainterTabChangedMessage(index, isSelectedTabWithDecals));
    }

    private void OnSpritePicked(string group, string style)
    {
        SendPredictedMessage(new SprayPainterSetPaintableStyleMessage(group, style));
    }

    private void OnSetPipeColor(ItemList.ItemListSelectedEventArgs args)
    {
        var key = _window?.IndexToColorKey(args.ItemIndex);
        SendPredictedMessage(new SprayPainterSetPipeColorMessage(key));
    }
}

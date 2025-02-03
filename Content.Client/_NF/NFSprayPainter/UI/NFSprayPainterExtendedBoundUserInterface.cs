using Content.Shared._NF.NFSprayPainter;
using Content.Shared._NF.NFSprayPainter.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._NF.NFSprayPainter.UI;

public sealed class NFSprayPainterExtendedBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NFSprayPainterWindow? _window;

    public NFSprayPainterExtendedBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NFSprayPainterWindow>();

        _window.OnSpritePicked = OnSpritePicked;
        _window.OnColorPicked = OnColorPicked;

        if (EntMan.TryGetComponent(Owner, out NFSprayPainterComponent? comp))
        {
            _window.Populate(EntMan.System<NFSprayPainterSystem>().Entries, comp.Indexes, comp.PickedColor, comp.ColorPalette);
        }
    }

    private void OnSpritePicked(string category, int index)
    {
        SendMessage(new NFSprayPainterSpritePickedMessage(category, index));
    }

    private void OnColorPicked(ItemList.ItemListSelectedEventArgs args)
    {
        var key = _window?.IndexToColorKey(args.ItemIndex);
        SendMessage(new NFSprayPainterColorPickedMessage(key));
    }
}

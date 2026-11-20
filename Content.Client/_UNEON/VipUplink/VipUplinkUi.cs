using Content.Client.UserInterface.Fragments;
using Content.Shared._UNEON.VipUplink;
using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._UNEON.VipUplink;

public sealed partial class VipUplinkUi : UIFragment
{
    private sealed class VipUplinkPanel : BoxContainer;
    private VipUplinkPanel _root = default!;
    private Label _balance = default!;
    private BoxContainer _products = default!;
    private BoundUserInterface _ui = default!;
    private readonly Dictionary<string, Button> _buttons = new();
    private string _catalogue = "";

    public override Control GetUIFragmentRoot() => _root;

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _ui = userInterface;
        _buttons.Clear();
        _catalogue = "";
        _root = new VipUplinkPanel { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
        _root.AddChild(new Label { Text = Loc.GetString("uneon-vip-title") });
        _balance = new Label { Text = Loc.GetString("uneon-vip-loading") };
        _root.AddChild(_balance);
        _products = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
        var scroll = new ScrollContainer { VerticalExpand = true, MinHeight = 200 };
        scroll.AddChild(_products);
        _root.AddChild(scroll);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not VipUplinkState vip)
            return;
        _balance.Text = vip.Allowed
            ? Loc.GetString("uneon-vip-balance", ("balance", vip.Balance))
            : Loc.GetString("uneon-vip-denied");
        // Do not rebuild buttons on every balance refresh: preserve focus, scroll and click state.
        var catalogue = string.Join("|", vip.Products.ConvertAll(p => $"{p.Id}:{p.Price}:{p.Name}"));
        if (_catalogue != catalogue)
        {
            _catalogue = catalogue;
            _products.RemoveAllChildren();
            _buttons.Clear();
            foreach (var product in vip.Products)
            {
                var button = new Button
                {
                    Text = Loc.GetString("uneon-vip-buy", ("name", Loc.GetString(product.Name)), ("price", product.Price)),
                    HorizontalExpand = true
                };
                button.OnPressed += _ =>
                {
                    button.Disabled = true;
                    _ui.SendMessage(new CartridgeUiMessage(new VipUplinkBuyMessage(product.Id)));
                };
                _buttons[product.Id] = button;
                _products.AddChild(button);
            }
        }
        foreach (var product in vip.Products)
            _buttons[product.Id].Disabled = !vip.Allowed || vip.Balance < product.Price;
    }
}

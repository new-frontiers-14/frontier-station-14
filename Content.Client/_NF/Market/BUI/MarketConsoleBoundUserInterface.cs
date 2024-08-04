using Content.Client._NF.Market.UI;
using Content.Shared._NF.Market.BUI;
using Content.Shared._NF.Market.Events;
using JetBrains.Annotations;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._NF.Market.BUI;

[UsedImplicitly]
public sealed class MarketConsoleBoundUserInterface : BoundUserInterface
{
    private MarketMenu? _menu;

    [ViewVariables]
    public int BankBalance { get; private set; }

    public MarketConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = new MarketMenu();
        //_menu.OnClose += Close;
        _menu.OnAddToCart1 += args => AddToCart(args, 1);
        _menu.OnAddToCart5 += args => AddToCart(args, 5);
        _menu.OnAddToCart10 += args => AddToCart(args, 10);
        _menu.OnAddToCart30 += args => AddToCart(args, 30);
        _menu.OnAddToCartAll += args => AddToCart(args, int.MaxValue);
        _menu.OnReturn += Return;
        _menu.OnPurchaseCart += PurchaseCrate;

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MarketConsoleInterfaceState uiState)
            return;

        if (_menu == null)
            return;

        BankBalance = uiState.Balance;

        _menu?.SetUiEnabled(uiState.Enabled);
        _menu?.UpdateState(uiState);
    }

    private void AddToCart(ButtonEventArgs args, int amount)
    {
        if (args.Button.Parent?.Parent?.Parent?.Parent is not MarketProductRow product)
            return;
        var addToCartMessage = new MarketConsoleCartMessage(amount, product.Prototype.ID);

        SendMessage(addToCartMessage);
    }

    private void Return(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent?.Parent is not MarketCartProductRow product)
            return;
        var purchaseMessage = new MarketConsoleCartMessage(0, product.Prototype.ID, true);

        SendMessage(purchaseMessage);
    }

    private void PurchaseCrate(ButtonEventArgs args)
    {
        SendMessage(new CrateMachinePurchaseMessage());
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}

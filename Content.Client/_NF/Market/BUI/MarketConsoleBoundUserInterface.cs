using Content.Client._NF.Market.UI;
using Content.Shared._NF.Market.BUI;
using Content.Shared._NF.Market.Events;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._NF.Market.BUI;

public sealed class MarketConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MarketMenu? _menu;

    [ViewVariables]
    public int BankBalance { get; private set; }

    public MarketConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = new MarketMenu();
        _menu.OnClose += Close;
        _menu.OnPurchase += Purchase;
        _menu.OnPurchaseCrate += PurchaseCrate;

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MarketConsoleInterfaceState uiState)
            return;

        BankBalance = uiState.Balance;

        _menu?.SetEnabled(uiState.Enabled);
        _menu?.UpdateState(uiState);
    }

    private void Purchase(ButtonEventArgs args)
    {
        // Do stuff
    }

    private void PurchaseCrate(ButtonEventArgs args)
    {
        SendMessage(new CrateMachinePurchaseMessage());
    }
}

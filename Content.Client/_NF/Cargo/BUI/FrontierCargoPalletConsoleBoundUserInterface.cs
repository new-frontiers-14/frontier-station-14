using Content.Client._NF.Cargo.UI;
using Content.Shared._NF.Cargo.BUI;
using Content.Shared._NF.Cargo.Events;

namespace Content.Client._NF.Cargo.BUI;

public sealed class FrontierCargoPalletConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private FrontierCargoPalletMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = new FrontierCargoPalletMenu();
        _menu.AppraiseRequested += OnAppraisal;
        _menu.SellRequested += OnSell;
        _menu.OnClose += Close;

        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _menu?.Dispose();
        }
    }

    private void OnAppraisal()
    {
        SendMessage(new FrontierCargoPalletAppraiseMessage());
    }

    private void OnSell()
    {
        SendMessage(new FrontierCargoPalletSellMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not FrontierCargoPalletConsoleInterfaceState palletState)
            return;

        _menu?.SetEnabled(palletState.Enabled);
        _menu?.SetAppraisal(palletState.Appraisal);
        _menu?.SetCount(palletState.Count);
    }
}

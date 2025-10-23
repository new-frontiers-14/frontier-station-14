using Content.Client.Cargo.UI;
using Content.Shared._NF.Cargo.BUI;
using Content.Shared.Cargo.Events;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Cargo.BUI;

// Suffixed to avoid BUI collisions (see RT#5648)
public sealed class CargoPalletConsoleNFBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CargoPalletMenu? _menu;

    public CargoPalletConsoleNFBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (_menu == null)
        {
            _menu = this.CreateWindow<CargoPalletMenu>();
            _menu.AppraiseRequested += OnAppraisal;
            _menu.SellRequested += OnSell;
        }
    }

    private void OnAppraisal()
    {
        SendMessage(new CargoPalletAppraiseMessage());
    }

    private void OnSell()
    {
        SendMessage(new CargoPalletSellMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NFCargoPalletConsoleInterfaceState palletState)
            return;

        _menu?.SetEnabled(palletState.Enabled);
        _menu?.SetAppraisal(palletState.Appraisal);
        _menu?.SetCount(palletState.Count);
    }
}

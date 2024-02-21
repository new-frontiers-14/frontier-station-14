using Content.Client._NF.Contraband.UI;
using Content.Shared._NF.Contraband.BUI;
using Content.Shared._NF.Contraband.Events;
using Robust.Shared.Utility;

namespace Content.Client._NF.Contraband.BUI;

public sealed class ContrabandPalletConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ContrabandPalletMenu? _menu;

    public ContrabandPalletConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        var disclaimer = new FormattedMessage();
        disclaimer.AddText(Loc.GetString($"contraband-pallet-disclaimer"));
        _menu = new ContrabandPalletMenu();
        _menu.AppraiseRequested += OnAppraisal;
        _menu.SellRequested += OnSell;
        _menu.OnClose += Close;
        _menu.Disclaimer.SetMessage(disclaimer);
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
        SendMessage(new ContrabandPalletAppraiseMessage());
    }

    private void OnSell()
    {
        SendMessage(new ContrabandPalletSellMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ContrabandPalletConsoleInterfaceState palletState)
            return;

        _menu?.SetEnabled(palletState.Enabled);
        _menu?.SetAppraisal(palletState.Appraisal);
        _menu?.SetCount(palletState.Count);
    }
}

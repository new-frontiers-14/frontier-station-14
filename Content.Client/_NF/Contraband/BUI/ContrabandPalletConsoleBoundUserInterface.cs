using Content.Client._NF.Contraband.UI;
using Content.Shared._NF.Contraband.BUI;
using Content.Shared._NF.Contraband.Components;
using Content.Shared._NF.Contraband.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._NF.Contraband.BUI;

public sealed class ContrabandPalletConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ContrabandPalletMenu? _menu;

    [ViewVariables]
    private string _locPrefix = string.Empty;

    public ContrabandPalletConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        if (EntMan.TryGetComponent<ContrabandPalletConsoleComponent>(owner, out var console))
            _locPrefix = console.LocStringPrefix ?? string.Empty;
    }

    protected override void Open()
    {
        base.Open();
        var disclaimer = new FormattedMessage();
        disclaimer.AddText(Loc.GetString($"{_locPrefix}contraband-pallet-disclaimer"));
        _menu = new ContrabandPalletMenu(_locPrefix);
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

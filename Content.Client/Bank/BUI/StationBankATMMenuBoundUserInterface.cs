using Content.Client.Bank.UI;
using Content.Shared.Bank.BUI;
using Content.Shared.Bank.Events;
using Robust.Client.GameObjects;
using Content.Shared.Access.Systems;

namespace Content.Client.Cargo.BUI;

public sealed class StationBankATMMenuBoundUserInterface : BoundUserInterface
{
    private StationBankATMMenu? _menu;

    public StationBankATMMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _menu = new StationBankATMMenu();
        _menu.WithdrawRequest += OnWithdraw;
        _menu.DepositRequest += OnDeposit;
        _menu.OnClose += Close;
        _menu.PopulateReasons();
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

    private void OnWithdraw()
    {
        if (_menu?.Amount is not ulong amount)
            return;

        SendMessage(new StationBankWithdrawMessage(amount, _menu.Reason, _menu.Description));
    }

    private void OnDeposit()
    {
        if (_menu?.Amount is not ulong amount)
            return;

        SendMessage(new StationBankDepositMessage(amount, _menu.Reason, _menu.Description));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not StationBankATMMenuInterfaceState bankState)
            return;

        _menu?.SetEnabled(bankState.Enabled);
        _menu?.SetBalance(bankState.Balance);
        _menu?.SetDeposit(bankState.Deposit);
    }
}

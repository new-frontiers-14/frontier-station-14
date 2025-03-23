using Content.Client._NF.Bank.UI;
using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Bank.Events;

namespace Content.Client.Cargo.BUI;

public sealed class BankATMMenuBoundUserInterface : BoundUserInterface
{
    private BankATMMenu? _menu;

    public BankATMMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _menu = new BankATMMenu();
        _menu.WithdrawRequest += OnWithdraw;
        _menu.DepositRequest += OnDeposit;
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

    private void OnWithdraw()
    {
        if (_menu?.Amount is not int amount)
            return;

        SendMessage(new BankWithdrawMessage(amount));
    }

    private void OnDeposit()
    {
        SendMessage(new BankDepositMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BankATMMenuInterfaceState bankState)
            return;

        _menu?.SetEnabled(bankState.Enabled);
        _menu?.SetBalance(bankState.Balance);
        _menu?.SetDeposit(bankState.Deposit);
    }
}

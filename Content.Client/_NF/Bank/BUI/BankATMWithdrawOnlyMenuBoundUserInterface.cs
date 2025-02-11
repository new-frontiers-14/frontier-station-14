using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Bank.Events;

namespace Content.Client._NF.Bank.UI;

public sealed class BankATMWithdrawOnlyMenuBoundUserInterface : BoundUserInterface
{
    private WithdrawBankATMMenu? _menu;

    public BankATMWithdrawOnlyMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _menu = new WithdrawBankATMMenu();
        _menu.WithdrawRequest += OnWithdraw;
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

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BankATMMenuInterfaceState bankState)
            return;

        _menu?.SetEnabled(bankState.Enabled);
        _menu?.SetBalance(bankState.Balance);
    }
}

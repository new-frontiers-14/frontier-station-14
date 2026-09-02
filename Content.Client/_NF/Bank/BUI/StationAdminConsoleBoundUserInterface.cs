using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Bank.Events;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Bank.UI;

public sealed class StationAdminConsoleBoundUserInterface : BoundUserInterface
{
    private StationAdminConsoleMenu? _menu;

    public StationAdminConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        if (_menu == null)
        {
            _menu = this.CreateWindow<StationAdminConsoleMenu>();
            _menu.WithdrawRequest += OnWithdraw;
            _menu.DepositRequest += OnDeposit;
            _menu.PopulateReasons();
        }
    }

    private void OnWithdraw()
    {
        if (_menu?.WithdrawalAmount is not int amount)
            return;

        SendMessage(new StationBankWithdrawMessage(amount, _menu.WithdrawalReason, _menu.WithdrawalDescription));
    }

    private void OnDeposit()
    {
        if (_menu?.DepositAmount is not int amount)
            return;

        SendMessage(new StationBankDepositMessage(amount, _menu.DepositReason, _menu.DepositDescription));
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

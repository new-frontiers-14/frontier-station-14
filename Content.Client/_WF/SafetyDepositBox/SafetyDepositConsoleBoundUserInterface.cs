using Content.Client.UserInterface.Controls;
using Content.Shared._WF.SafetyDepositBox.BUI;
using Content.Shared._WF.SafetyDepositBox.Events;
using Robust.Client.UserInterface;

namespace Content.Client._WF.SafetyDepositBox;

public sealed class SafetyDepositConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SafetyDepositConsoleWindow? _window;

    public SafetyDepositConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SafetyDepositConsoleWindow>();
        _window.Title = Loc.GetString("safety-deposit-console-title");
        _window.OnPurchasePressed += OnPurchasePressed;
        _window.OnDepositPressed += OnDepositPressed;
        _window.OnWithdrawPressed += OnWithdrawPressed;
        _window.OnReclaimPressed += OnReclaimPressed;
    }

    private void OnPurchasePressed(SafetyDepositBoxSize boxSize)
    {
        SendMessage(new SafetyDepositPurchaseMessage(boxSize));
    }

    private void OnDepositPressed()
    {
        SendMessage(new SafetyDepositDepositMessage());
    }

    private void OnWithdrawPressed(Guid boxId)
    {
        SendMessage(new SafetyDepositWithdrawMessage(boxId));
    }

    private void OnReclaimPressed(Guid boxId)
    {
        SendMessage(new SafetyDepositReclaimMessage(boxId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SafetyDepositConsoleState castState)
            return;

        _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}

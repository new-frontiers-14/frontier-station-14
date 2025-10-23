using Robust.Client.UserInterface;
using static Content.Shared._NF.Atmos.Components.GasDepositScannerComponent;

namespace Content.Client._NF.Atmos.UI;

public sealed class GasDepositScannerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GasDepositScannerWindow? _window;

    public GasDepositScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            _window = this.CreateWindowCenteredLeft<GasDepositScannerWindow>();
            _window.OnClose += OnClose;
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;
        if (message is not GasDepositScannerUserMessage cast)
            return;
        _window.Populate(cast);
    }

    /// <summary>
    /// Closes UI and tells the server to disable the analyzer
    /// </summary>
    private void OnClose()
    {
        SendMessage(new GasDepositScannerDisableMessage());
        Close();
    }
}

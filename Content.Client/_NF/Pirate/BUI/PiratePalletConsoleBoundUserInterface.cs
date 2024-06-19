using Content.Client._NF.Pirate.UI;
using Content.Shared._NF.Pirate.BUI;
using Content.Shared._NF.Pirate.Events;
using Robust.Client.GameObjects;

namespace Content.Client._NF.Pirate.BUI;

public sealed class PiratePalletConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PiratePalletMenu? _menu;

    public PiratePalletConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new PiratePalletMenu();
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

    private void OnSell()
    {
        SendMessage(new PiratePalletSellMessage());
    }

    // TODO: remove this, nothing to update
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not PiratePalletConsoleInterfaceState palletState)
            return;
    }
}

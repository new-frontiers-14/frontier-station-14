using Content.Client._NF.Pirate.UI;
using Content.Shared._NF.Pirate.BUI;
using Content.Shared._NF.Pirate.Events;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Pirate.BUI;

public sealed class PirateBountyRedemptionConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PirateBountyRedemptionMenu? _menu;

    public PirateBountyRedemptionConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        if (_menu == null)
        {
            _menu = this.CreateWindow<PirateBountyRedemptionMenu>();
            _menu.SellRequested += OnSell;
        }
    }

    private void OnSell()
    {
        SendMessage(new PirateBountyRedemptionMessage());
    }

    // TODO: remove this, nothing to update
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not PirateBountyRedemptionConsoleInterfaceState palletState)
            return;
    }
}

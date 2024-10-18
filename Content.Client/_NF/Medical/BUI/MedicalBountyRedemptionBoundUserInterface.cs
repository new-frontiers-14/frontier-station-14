using JetBrains.Annotations;
using Content.Client._NF.Medical.UI;
using Content.Shared._NF.Medical;

namespace Content.Client._NF.Medical.BUI;

[UsedImplicitly]
public sealed class MedicalBountyRedemptionBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MedicalBountyRedemptionMenu? _menu;

    public MedicalBountyRedemptionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new();

        _menu.OnClose += Close;

        _menu.SellRequested += SendBountyMessage;

        _menu.OpenCentered();
    }

    private void SendBountyMessage()
    {
        SendMessage(new RedeemMedicalBountyMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not MedicalBountyRedemptionUIState state)
            return;

        _menu?.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}

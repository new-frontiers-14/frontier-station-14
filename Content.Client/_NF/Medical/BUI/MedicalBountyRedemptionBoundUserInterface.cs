using JetBrains.Annotations;
using Content.Client._NF.Medical.UI;
using Content.Shared._NF.Medical;
using Robust.Client.UserInterface;

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

        if (_menu == null)
        {
            _menu = this.CreateWindow<MedicalBountyRedemptionMenu>();
            _menu.SellRequested += SendBountyMessage;
        }
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
}

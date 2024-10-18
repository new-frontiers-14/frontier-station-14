using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Prototypes;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class NF_ShipConnectUi : UIFragment
{
    private NF_ShipConnectUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NF_ShipConnectUiFragment();

        //_fragment.OnSync += _ => SendSyncMessage(userInterface);
    }

    private void SendSyncMessage(BoundUserInterface userInterface)
    {
        //var syncMessage = new NF_ShipConnectSyncMessageEvent();
        //var message = new CartridgeUiMessage(syncMessage);
        //userInterface.SendMessage(message);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
    }
}

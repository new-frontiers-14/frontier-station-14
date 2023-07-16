using Content.Client.UserInterface.Fragments;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._NF.BountyContracts.UI;

public sealed class BountyContractUi : UIFragment
{
    private BountyContractUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new BountyContractUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
    }
}

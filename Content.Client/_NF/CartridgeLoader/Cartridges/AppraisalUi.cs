using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._NF.CartridgeLoader.Cartridges;

public sealed partial class AppraisalUi : UIFragment
{
    private AppraisalUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new AppraisalUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not AppraisalUiState appraisalUiState)
            return;

        _fragment?.UpdateState(appraisalUiState.AppraisedItems);
    }
}

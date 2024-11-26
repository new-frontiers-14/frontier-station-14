using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared._NF.Bank.BUI;

namespace Content.Client._NF.CartridgeLoader.Cartridges;

public sealed partial class LedgerUi : UIFragment
{
    private LedgerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new LedgerUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NFLedgerState cast)
        {
            _fragment?.UpdateState(cast);
        }
    }
}

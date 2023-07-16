using Content.Client.UserInterface.Fragments;
using Content.Shared.StationBounties;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._NF.BountyContracts.UI;

public sealed class BountyContractUi : UIFragment
{
    private BountyContractUiFragment? _fragment;
    private BoundUserInterface? _userInterface;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new BountyContractUiFragment();
        _userInterface = userInterface;
        _fragment.ListMenu.OnCreateButtonPressed += OnListCreateButton;
    }

    private void OnListCreateButton()
    {
        _userInterface?.SendMessage(new BountyContractListCreateMsg());
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (_fragment == null)
            return;

        if (state is BountyContractListUiState listState)
        {
            _fragment.ListMenu.SetContracts(listState.Contracts);
            _fragment.ShowSubmenu(BountyContractFragmentState.List);
        }
        else if (state is BountyContractCreateUiState createState)
        {
            _fragment.CreateMenu.SetPossibleTargets(createState.Targets);
            _fragment.CreateMenu.SetVessels(createState.Vessels);
            _fragment.ShowSubmenu(BountyContractFragmentState.Create);
        }

    }
}

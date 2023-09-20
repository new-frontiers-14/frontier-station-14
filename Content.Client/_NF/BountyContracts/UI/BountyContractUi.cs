using Content.Client.UserInterface.Fragments;
using Content.Shared._NF.BountyContracts;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._NF.BountyContracts.UI;

[UsedImplicitly]
public sealed partial class BountyContractUi : UIFragment
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
    }


    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (_fragment == null)
            return;

        if (state is BountyContractListUiState listState)
        {
            ShowListState(listState);
        }
        else if (state is BountyContractCreateUiState createState)
        {
            ShowCreateState(createState);
        }
    }

    private void UnloadPreviousState()
    {
        _fragment?.RemoveAllChildren();
    }

    private void ShowCreateState(BountyContractCreateUiState state)
    {
        UnloadPreviousState();

        var create = new BountyContractUiFragmentCreate();
        create.OnCancelPressed += OnCancelCreatePressed;
        create.OnCreatePressed += OnTryCreatePressed;

        create.SetPossibleTargets(state.Targets);
        create.SetVessels(state.Vessels);

        _fragment?.AddChild(create);
    }

    private void ShowListState(BountyContractListUiState state)
    {
        UnloadPreviousState();

        var list = new BountyContractUiFragmentList();
        list.OnCreateButtonPressed += OnOpenCreateUiPressed;
        list.OnRefreshButtonPressed += OnRefreshListPressed;
        list.OnRemoveButtonPressed += OnRemovePressed;

        list.SetContracts(state.Contracts, state.IsAllowedRemoveBounties);
        list.SetCanCreate(state.IsAllowedCreateBounties);

        _fragment?.AddChild(list);
    }

    private void OnRemovePressed(BountyContract obj)
    {
        _userInterface?.SendMessage(new BountyContractTryRemoveUiMsg(obj.ContractId));
    }

    private void OnRefreshListPressed()
    {
        _userInterface?.SendMessage(new BountyContractRefreshListUiMsg());
    }

    private void OnOpenCreateUiPressed()
    {
        _userInterface?.SendMessage(new BountyContractOpenCreateUiMsg());
    }

    private void OnCancelCreatePressed()
    {
        _userInterface?.SendMessage(new BountyContractCloseCreateUiMsg());
    }

    private void OnTryCreatePressed(BountyContractRequest contract)
    {
        _userInterface?.SendMessage(new BountyContractTryCreateMsg(contract));
    }
}

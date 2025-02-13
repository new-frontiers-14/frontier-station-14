using Content.Client.UserInterface.Fragments;
using Content.Shared._NF.BountyContracts;
using Content.Shared.CartridgeLoader;
using JetBrains.Annotations;
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
            ShowListState(listState);
        else if (state is BountyContractCreateUiState createState)
            ShowCreateState(createState);
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

    // UI event handlers
    private void OnRemovePressed(BountyContract obj)
    {
        SendMessage(new BountyContractTryRemoveMessageEvent(obj.ContractId));
    }

    private void OnRefreshListPressed()
    {
        SendMessage(MakeCommand(BountyContractCommand.RefreshList));
    }

    private void OnOpenCreateUiPressed()
    {
        SendMessage(MakeCommand(BountyContractCommand.OpenCreateUi));
    }

    private void OnCancelCreatePressed()
    {
        SendMessage(MakeCommand(BountyContractCommand.CloseCreateUi));
    }

    private void OnTryCreatePressed(BountyContractRequest contract)
    {
        SendMessage(new BountyContractTryCreateMessageEvent(contract));
    }

    // Convenience functions for message creation
    private BountyContractCommandMessageEvent MakeCommand(BountyContractCommand command)
    {
        return new BountyContractCommandMessageEvent(command);
    }

    private void SendMessage(CartridgeMessageEvent msg)
    {
        _userInterface?.SendMessage(new CartridgeUiMessage(msg));
    }
}

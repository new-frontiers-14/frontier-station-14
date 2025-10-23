using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared._NF.BountyContracts;
using Content.Shared.CartridgeLoader;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.BountyContracts.UI;

[UsedImplicitly]
public sealed partial class BountyContractUi : UIFragment
{
    private BountyContractUiFragment? _fragment;
    private BoundUserInterface? _userInterface;
    private ProtoId<BountyContractCollectionPrototype> _lastCollection = "Command"; //FIXME: nasty.
    private BoundUserInterfaceState? _lastState = null;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new BountyContractUiFragment();
        _userInterface = userInterface;
        _lastState = null;
    }


    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (_fragment == null)
            return;

        if (state is BountyContractListUiState listState)
            ShowListState(listState);
        else if (state is BountyContractCreateUiState createState)
            ShowCreateState(createState);
        _lastState = state;
    }

    private void UnloadPreviousState()
    {
        _fragment?.RemoveAllChildren();
    }

    private void ShowCreateState(BountyContractCreateUiState state)
    {
        // If the previous state is already a create state, do not destroy our old state.
        if (_lastState is BountyContractCreateUiState)
            return;

        UnloadPreviousState();

        var create = new BountyContractUiFragmentCreate(state.Collection);
        create.OnCancelPressed += OnCancelCreatePressed;
        create.OnCreatePressed += OnTryCreatePressed;

        create.SetPossibleTargets(state.Targets);
        create.SetVessels(state.Vessels);

        _fragment?.AddChild(create);
    }

    private void ShowListState(BountyContractListUiState state)
    {
        UnloadPreviousState();
        var tabs = new BountyContractUiFragmentTabSet();
        tabs.OnSelectCollection += OnSelectCollection;
        state.Collections.Reverse(); // invert order, show latest first
        foreach (var collection in state.Collections)
        {
            int newTabIndex = tabs.Children.Count();
            if (collection == state.Collection)
            {
                var list = new BountyContractUiFragmentList();
                list.OnCreateButtonPressed += OnOpenCreateUiPressed;
                list.OnRefreshButtonPressed += OnRefreshListPressed;
                list.OnRemoveButtonPressed += OnRemovePressed;
                list.OnToggleNotificationPressed += OnToggleNotificationPressed;
                list.SetContracts(state.Contracts, state.IsAllowedRemoveBounties, state.AuthorUid);
                list.SetCanCreate(state.IsAllowedCreateBounties);
                list.SetNotificationsEnabled(state.NotificationsEnabled);
                tabs.Children.Add(list);

                tabs.CurrentTab = newTabIndex;
                tabs.SetTabCollection(newTabIndex, collection);
            }
            else
            {
                var placeholder = new BountyContractUiFragmentListPlaceholder();
                tabs.Children.Add(placeholder);
                tabs.SetTabCollection(newTabIndex, collection);
            }
        }

        _fragment?.AddChild(tabs);
        _lastCollection = state.Collection;
    }

    // UI event handlers
    private void OnRemovePressed(BountyContract obj)
    {
        SendMessage(new BountyContractTryRemoveMessageEvent(obj.ContractId));
    }

    private void OnSelectCollection(ProtoId<BountyContractCollectionPrototype> collection)
    {
        SendMessage(MakeCommand(BountyContractCommand.RefreshList, collection));
    }

    private void OnRefreshListPressed()
    {
        SendMessage(MakeCommand(BountyContractCommand.RefreshList, _lastCollection));
    }

    private void OnOpenCreateUiPressed()
    {
        SendMessage(MakeCommand(BountyContractCommand.OpenCreateUi, _lastCollection));
    }

    private void OnCancelCreatePressed()
    {
        SendMessage(MakeCommand(BountyContractCommand.CloseCreateUi, _lastCollection));
    }

    private void OnToggleNotificationPressed()
    {
        SendMessage(MakeCommand(BountyContractCommand.ToggleNotifications, _lastCollection));
    }

    private void OnTryCreatePressed(BountyContractRequest contract)
    {
        SendMessage(new BountyContractTryCreateMessageEvent(contract));
    }

    // Convenience functions for message creation
    private BountyContractCommandMessageEvent MakeCommand(BountyContractCommand command, ProtoId<BountyContractCollectionPrototype> collection)
    {
        return new BountyContractCommandMessageEvent(command, collection);
    }

    private void SendMessage(CartridgeMessageEvent msg)
    {
        _userInterface?.SendMessage(new CartridgeUiMessage(msg));
    }
}

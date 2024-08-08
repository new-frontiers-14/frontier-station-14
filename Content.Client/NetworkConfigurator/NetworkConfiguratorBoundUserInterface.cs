﻿using Content.Client.NetworkConfigurator.Systems;
using Content.Shared.DeviceNetwork;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorBoundUserInterface : BoundUserInterface
{
    private readonly NetworkConfiguratorSystem _netConfig;

    [ViewVariables]
    private NetworkConfiguratorConfigurationMenu? _configurationMenu;

    [ViewVariables]
    private NetworkConfiguratorLinkMenu? _linkMenu;

    [ViewVariables]
    private NetworkConfiguratorListMenu? _listMenu;

    public NetworkConfiguratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _netConfig = EntMan.System<NetworkConfiguratorSystem>();
    }

    public void OnRemoveButtonPressed(string address)
    {
        SendMessage(new NetworkConfiguratorRemoveDeviceMessage(address));
    }

    protected override void Open()
    {
        base.Open();

        switch (UiKey)
        {
            case NetworkConfiguratorUiKey.List:
                _listMenu = new NetworkConfiguratorListMenu(this);
                _listMenu.OnClose += Close;
                _listMenu.ClearButton.OnPressed += _ => OnClearButtonPressed();
                _listMenu.OpenCenteredRight();
                break;
            case NetworkConfiguratorUiKey.Configure:
                _configurationMenu = new NetworkConfiguratorConfigurationMenu();
                _configurationMenu.OnClose += Close;
                _configurationMenu.Set.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Set);
                _configurationMenu.Add.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Add);
                //_configurationMenu.Edit.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Edit);
                _configurationMenu.Clear.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Clear);
                _configurationMenu.Copy.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Copy);
                _configurationMenu.Show.OnPressed += OnShowPressed;
                _configurationMenu.Show.Pressed = _netConfig.ConfiguredListIsTracked(Owner);
                _configurationMenu.OpenCentered();
                break;
            case NetworkConfiguratorUiKey.Link:
                _linkMenu = new NetworkConfiguratorLinkMenu(this);
                _linkMenu.OnClose += Close;
                _linkMenu.OpenCentered();
                break;
        }
    }

    private void OnShowPressed(BaseButton.ButtonEventArgs args)
    {
        _netConfig.ToggleVisualization(Owner, args.Button.Pressed);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case NetworkConfiguratorUserInterfaceState configState:
                _listMenu?.UpdateState(configState);
                break;
            case DeviceListUserInterfaceState listState:
                _configurationMenu?.UpdateState(listState);
                break;
            case DeviceLinkUserInterfaceState linkState:
                _linkMenu?.UpdateState(linkState);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _linkMenu?.Dispose();
        _listMenu?.Dispose();
        _configurationMenu?.Dispose();
    }

    private void OnClearButtonPressed()
    {
        SendMessage(new NetworkConfiguratorClearDevicesMessage());
    }

    private void OnConfigButtonPressed(NetworkConfiguratorButtonKey buttonKey)
    {
        SendMessage(new NetworkConfiguratorButtonPressedMessage(buttonKey));
    }
}

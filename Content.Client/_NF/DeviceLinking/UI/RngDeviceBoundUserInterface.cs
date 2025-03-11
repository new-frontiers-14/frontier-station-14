using Content.Shared._NF.DeviceLinking;
using Robust.Client.GameObjects;
using Content.Client._NF.DeviceLinking.UI;
using Robust.Client.UserInterface;

namespace Content.Client._NF.DeviceLinking.UI;

public sealed class RngDeviceBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RngDeviceWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RngDeviceWindow>();
        _window.OnMuteToggled += OnMuteToggled;
        _window.OnEdgeModeToggled += OnEdgeModeToggled;
        _window.OnTargetNumberChanged += OnTargetNumberChanged;
        _window.OpenCentered();
    }

    private void OnMuteToggled(bool muted)
    {
        SendMessage(new RngDeviceToggleMuteMessage(muted));
    }

    private void OnEdgeModeToggled(bool edgeMode)
    {
        SendMessage(new RngDeviceToggleEdgeModeMessage(edgeMode));
    }

    private void OnTargetNumberChanged(int targetNumber)
    {
        SendMessage(new RngDeviceSetTargetNumberMessage(targetNumber));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RngDeviceBoundUserInterfaceState rngState)
            return;

        _window?.UpdateState(rngState);
    }
}

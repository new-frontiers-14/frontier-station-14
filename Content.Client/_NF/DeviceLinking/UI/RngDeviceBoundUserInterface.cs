using Content.Client._NF.DeviceLinking.Systems;
using Content.Shared._NF.DeviceLinking;
using Content.Shared._NF.DeviceLinking.Components;
using Robust.Client.GameObjects;
using Content.Client._NF.DeviceLinking.UI;
using Robust.Client.UserInterface;
using Content.Shared.UserInterface;

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

        if (_window == null || state is not RngDeviceBoundUserInterfaceState cast)
            return;

        _window.SetMuted(cast.Muted);
        _window.SetEdgeMode(cast.EdgeMode);
        _window.SetTargetNumber(cast.TargetNumber);
        _window.SetTargetNumberVisibility(cast.Outputs == 2);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}

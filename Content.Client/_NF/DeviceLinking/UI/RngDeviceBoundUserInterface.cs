using Content.Shared.DeviceLinking;
using Robust.Client.GameObjects;
using Content.Client._NF.DeviceLinking.UI;
using Robust.Client.UserInterface;

namespace Content.Client._NF.DeviceLinking.UI;

public sealed class RngDeviceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RngDeviceWindow? _window;

    public RngDeviceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RngDeviceWindow>();
        _window.OnMuteToggled += OnMuteToggled;
        _window.OpenCentered();
    }

    private void OnMuteToggled(bool muted)
    {
        SendMessage(new RngDeviceToggleMuteMessage(muted));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RngDeviceBoundUserInterfaceState rngState)
            return;

        _window?.UpdateState(rngState);
    }
}

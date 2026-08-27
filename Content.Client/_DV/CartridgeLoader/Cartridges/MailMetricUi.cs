using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization;

namespace Content.Client._DV.CartridgeLoader.Cartridges;

public sealed partial class MailMetricUi : UIFragment
{
    private MailMetricUiFragment? _fragment;
    private BoundUserInterface _userInterface;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MailMetricUiFragment();
        _userInterface = userInterface;
        _fragment.OnToggleNotificationButtonPressed += OnNotificationToggled;

    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is MailMetricUiState cast)
        {
            _fragment?.UpdateState(cast);
        }
    }

    public void OnNotificationToggled()
    {
        if (_fragment == null)
            return;
        var newStatus = !_fragment.MailNotificationEnabledButtonStatus;
        _fragment.SetNotificationButtonVisual(newStatus);
        var message = new MailMetricsNotificationToggleMessage(newStatus);
        _userInterface.SendMessage(new CartridgeUiMessage(message));
    }
}


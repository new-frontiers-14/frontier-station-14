using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader; // Frontier
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Client._DV.CartridgeLoader.Cartridges;

public sealed partial class MailMetricUi : UIFragment
{
    private MailMetricUiFragment? _fragment;
    private BoundUserInterface _userInterface; // Frontier: Required to send messages to the server

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MailMetricUiFragment();
        _userInterface = userInterface; // Frontier: Save the UI so we can send messages to the server
        _fragment.OnToggleNotificationButtonPressed += OnNotificationToggled; // Frontier: Detect when the notification button is pressed

    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is MailMetricUiState cast)
        {
            _fragment?.UpdateState(cast);
        }
    }

    // Frontier: Add a method to toggle the visual notification button.
    // Keep in mind that this is solely a visual toggle, as the data for this program is only stored server side
    public void OnNotificationToggled()
    {
        if (_fragment == null)
            return;
        var newStatus = !_fragment.MailNotificationEnabledButtonStatus;
        _fragment.SetNotificationButtonVisual(newStatus);
        var message = new MailMetricsNotificationToggleMessage(newStatus);
        _userInterface.SendMessage(new CartridgeUiMessage(message));
    }
    //End Frontier
}

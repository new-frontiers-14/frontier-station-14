using Content.Client._NF.DeviceLinking.Systems;
using Content.Shared._NF.DeviceLinking;
using Content.Shared._NF.DeviceLinking.Components;
using Robust.Client.GameObjects;
using Content.Client._NF.DeviceLinking.UI;
using Robust.Client.UserInterface;

namespace Content.Client._NF.DeviceLinking.UI;

public sealed class RngDeviceBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RngDeviceWindow? _window;

    [Dependency] private readonly IEntityManager _entityManager = default!;

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
        // We can't modify the component directly on the client
        // Just send the message to the server
        SendMessage(new RngDeviceToggleMuteMessage(muted));
    }

    private void OnEdgeModeToggled(bool edgeMode)
    {
        // We can't modify the component directly on the client
        // Just send the message to the server
        SendMessage(new RngDeviceToggleEdgeModeMessage(edgeMode));
    }

    private void OnTargetNumberChanged(int targetNumber)
    {
        // We can't modify the component directly on the client
        // Just send the message to the server

        // If we have a client system, predict a roll with the new target number
        if (_entityManager.TryGetComponent<TransformComponent>(Owner, out _) &&
            _entityManager.EntitySysManager.TryGetEntitySystem<RngDeviceSystem>(out var system) &&
            _entityManager.TryGetComponent<RngDeviceComponent>(Owner, out var component))
        {
            system.PredictRoll(new Entity<RngDeviceComponent>(Owner, component));
        }

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

using Content.Client._NF.Atmos.UI;
using Content.Shared._NF.Atmos.BUIStates;
using Content.Shared._NF.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Localizations;
using Content.Shared.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Atmos.BUI;

/// <summary>
/// Initializes a <see cref="GaslockConsoleBoundUserInterface"/>.
/// </summary>
[UsedImplicitly]
public sealed class GaslockConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private const float MaxPressure = Atmospherics.MaxOutputPressure;

    [ViewVariables]
    private GaslockConsoleWindow? _window;

    public GaslockConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GaslockConsoleWindow>();

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.TogglePumpDirectionButtonPressed += OnToggleDirectionButtonPressed;
        _window.PumpOutputPressureChanged += OnPumpOutputPressurePressed;

        _window.DockRequest += OnDockRequest;
        _window.UndockRequest += OnUndockRequest;
    }

    private void OnToggleStatusButtonPressed()
    {
        if (_window is null) return;
        SendMessage(new GasPressurePumpToggleStatusMessage(_window.PumpStatus));
    }

    private void OnToggleDirectionButtonPressed()
    {
        if (_window is null) return;
        SendMessage(new GasPressurePumpChangePumpDirectionMessage(_window.PumpInwards));
    }

    private void OnPumpOutputPressurePressed(string value)
    {
        var pressure = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
        if (pressure > MaxPressure) pressure = MaxPressure;

        SendMessage(new GasPressurePumpChangeOutputPressureMessage(pressure));
    }

    private void OnUndockRequest(NetEntity entity)
    {
        SendMessage(new UndockRequestMessage()
        {
            DockEntity = entity,
        });
    }

    private void OnDockRequest(NetEntity entity, NetEntity target)
    {
        SendMessage(new DockRequestMessage()
        {
            DockEntity = entity,
            TargetDockEntity = target,
        });
    }

    /// <summary>
    /// Update the UI state based on server-sent info
    /// </summary>
    /// <param name="state">New state to update the window.</param>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not GaslockConsoleBoundUserInterfaceState cast)
            return;

        _window.UpdateState(Owner, cast);
    }
}

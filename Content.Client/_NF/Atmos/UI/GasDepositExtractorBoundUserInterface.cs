using Content.Client.Atmos.UI;
using Content.Server._NF.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Atmos.UI;

/// <summary>
/// Initializes a <see cref="GasPressurePumpWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class GasDepositExtractorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GasPressurePumpWindow? _window;

    public GasDepositExtractorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GasPressurePumpWindow>();

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.PumpOutputPressureChanged += OnPumpOutputPressurePressed;
        Update();
    }

    public void Update()
    {
        if (_window == null)
            return;

        _window.Title = Identity.Name(Owner, EntMan);

        if (!EntMan.TryGetComponent(Owner, out GasDepositExtractorComponent? pump))
            return;

        _window.SetPumpStatus(pump.Enabled);
        _window.MaxPressure = pump.MaxTargetPressure;
        _window.SetOutputPressure(pump.TargetPressure);
    }

    private void OnToggleStatusButtonPressed()
    {
        if (_window is null) return;
        SendPredictedMessage(new GasPressurePumpToggleStatusMessage(_window.PumpStatus));
    }

    private void OnPumpOutputPressurePressed(float value)
    {
        SendPredictedMessage(new GasPressurePumpChangeOutputPressureMessage(value));
    }
}

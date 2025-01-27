using Content.Shared._NF.Atmos.Piping.Binary.Messages;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="GasPressureBidiPumpWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class GasPressureBidiPumpBoundUserInterface(EntityUid owner, Enum uiKey)
        : BoundUserInterface(owner, uiKey)
    {
        [ViewVariables]
        private GasPressureBidiPumpWindow? _window;

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<GasPressureBidiPumpWindow>();

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.ToggleDirectionButtonPressed += OnToggleDirectionButtonPressed;
            _window.PumpOutputPressureChanged += OnPumpOutputPressurePressed;
            Update();
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null)
                return;

            SendMessage(new GasPressurePumpToggleStatusMessage(_window.PumpStatus));
        }

        private void OnToggleDirectionButtonPressed()
        {
            if (_window is null)
                return;

            SendMessage(new GasPressurePumpChangePumpDirectionMessage(_window.PumpInwards));
        }

        private void OnPumpOutputPressurePressed(float value)
        {
            SendMessage(new GasPressurePumpChangeOutputPressureMessage(value));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        public override void Update()
        {
            if (_window == null)
                return;

            _window.Title = Identity.Name(Owner, EntMan);

            if (!EntMan.TryGetComponent(Owner, out GasPressurePumpComponent? pump))
                return;

            _window.SetPumpStatus(pump.Enabled);
            _window.MaxPressure = pump.MaxTargetPressure;
            _window.SetOutputPressure(pump.TargetPressure);
            _window.SetPumpDirection(pump.PumpingInwards);
        }
    }
}

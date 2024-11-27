using Content.Shared._NF.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Atmos.UI
{
    /// <summary>
    /// Initializes a <see cref="BidiGasPressurePumpWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class BidiGasPressurePumpBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private const float MaxPressure = Atmospherics.MaxOutputPressure;

        [ViewVariables]
        private BidiGasPressurePumpWindow? _window;

        public BidiGasPressurePumpBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<BidiGasPressurePumpWindow>();

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.ToggleDirectionButtonPressed += OnToggleDirectionButtonPressed;
            _window.PumpOutputPressureChanged += OnPumpOutputPressurePressed;
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

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasPressurePumpBoundUserInterfaceState cast)
                return;

            _window.Title = cast.PumpLabel;
            _window.SetPumpStatus(cast.Enabled);
            _window.SetOutputPressure(cast.OutputPressure);
            _window.SetPumpDirection(cast.Inward);
        }
    }
}

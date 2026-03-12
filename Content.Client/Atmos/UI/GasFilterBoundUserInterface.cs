
using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI
{
    [UsedImplicitly]
    public sealed class GasFilterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private const float MaxTransferRate = Atmospherics.MaxTransferRate;

        [ViewVariables]
        private GasFilterWindow? _window;

        public GasFilterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var atmosSystem = EntMan.System<AtmosphereSystem>();

            _window = this.CreateWindow<GasFilterWindow>();
            _window.PopulateGasList(atmosSystem.Gases);

            _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
            _window.FilterTransferRateChanged += OnFilterTransferRatePressed;
            _window.FilterGasesChanged += OnFilterGasesChanged;
            // Funky Station - Function and variable names changed to reflect multigas filtering
        }

        private void OnToggleStatusButtonPressed()
        {
            if (_window is null) return;
            SendMessage(new GasFilterToggleStatusMessage(_window.FilterStatus));
        }

        private void OnFilterTransferRatePressed(string value)
        {
            var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
            SendMessage(new GasFilterChangeRateMessage(rate));
        }

        private void OnFilterGasesChanged(HashSet<Gas> gases)
        {
            SendMessage(new GasFilterChangeGasesMessage(gases));
        }
        // Funky Station - Change of state in UI broadcasts hashset of gases
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not GasFilterBoundUserInterfaceState cast)
                return;

            _window.Title = cast.FilterLabel;
            _window.SetFilterStatus(cast.Enabled);
            _window.SetTransferRate(cast.TransferRate);
            _window.SetFilteredGases(cast.FilterGases ?? new HashSet<Gas>());
            // Funky Station - UI updated using hashset of gases
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}

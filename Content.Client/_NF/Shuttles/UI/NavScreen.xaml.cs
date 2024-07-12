using Content.Shared._NF.Shuttles.Events;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Shuttles.UI
{
    public sealed partial class NavScreen
    {
        private readonly ButtonGroup _buttonGroup = new();
        public event Action<NetEntity?, InertiaDampeningMode>? OnChangeInertiaDampeningTypeRequest;

        private void NfInitialize()
        {
            // Frontier - IFF search
            IffSearchCriteria.OnTextChanged += args => OnIffSearchChanged(args.Text);

            // Frontier - Maximum IFF Distance
            MaximumIFFDistanceValue.GetChild(0).GetChild(1).Margin = new Thickness(8, 0, 0, 0);
            MaximumIFFDistanceValue.OnValueChanged += args => OnRangeFilterChanged(args);

            DampenerOff.OnPressed += _ => SwitchDampenerMode(InertiaDampeningMode.Off);
            DampenerOn.OnPressed += _ => SwitchDampenerMode(InertiaDampeningMode.Dampen);
            AnchorOn.OnPressed += _ => SwitchDampenerMode(InertiaDampeningMode.Anchor);

            DampenerOff.Group = _buttonGroup;
            DampenerOn.Group = _buttonGroup;
            AnchorOn.Group = _buttonGroup;
        }

        private void SwitchDampenerMode(InertiaDampeningMode mode)
        {
            NavRadar.DampeningMode = mode;
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnChangeInertiaDampeningTypeRequest?.Invoke(shuttle, mode);
        }

        private void NfUpdateState()
        {
            if (NavRadar.DampeningMode == InertiaDampeningMode.Station)
            {
                DampenerModeButtons.Visible = false;
            }
            else
            {
                DampenerModeButtons.Visible = true;
                DampenerOff.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Off;
                DampenerOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Dampen;
                AnchorOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Anchor;
            }
        }

        // Frontier - Maximum IFF Distance
        private void OnRangeFilterChanged(int value)
        {
            NavRadar.MaximumIFFDistance = (float) value;
        }

        private void NfAddShuttleDesignation(EntityUid? shuttle)
        {
            // Frontier - PR #1284 Add Shuttle Designation
            if (_entManager.TryGetComponent<MetaDataComponent>(shuttle, out var metadata))
            {
                var shipNameParts = metadata.EntityName.Split(' ');
                var designation = shipNameParts[^1];
                if (designation[2] == '-')
                {
                    NavDisplayLabel.Text = string.Join(' ', shipNameParts[..^1]);
                    ShuttleDesignation.Text = designation;
                }
                else
                    NavDisplayLabel.Text = metadata.EntityName;
            }
            // End Frontier - PR #1284
        }

    }
}

// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using System.Numerics;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Shuttles.UI
{
    public sealed partial class NavScreen
    {
        private readonly ButtonGroup _buttonGroup = new();
        public event Action<NetEntity?, InertiaDampeningMode>? OnInertiaDampeningModeChanged;
        public event Action<NetEntity?, ServiceFlags>? OnServiceFlagsChanged;
        public event Action<NetEntity?, Vector2>? OnSetTargetCoordinates;
        public event Action<NetEntity?, bool>? OnSetHideTarget;

        private bool _targetCoordsModified = false;

        private void NfInitialize()
        {
            IffSearchCriteria.OnTextChanged += args => OnIffSearchChanged(args.Text);

            MaximumIFFDistanceValue.GetChild(0).GetChild(1).Margin = new Thickness(8, 0, 0, 0);
            MaximumIFFDistanceValue.OnValueChanged += args => OnRangeFilterChanged(args);

            DampenerOff.OnPressed += _ => SetDampenerMode(InertiaDampeningMode.Off);
            DampenerOn.OnPressed += _ => SetDampenerMode(InertiaDampeningMode.Dampen);
            AnchorOn.OnPressed += _ => SetDampenerMode(InertiaDampeningMode.Anchor);

            DampenerOff.Group = _buttonGroup;
            DampenerOn.Group = _buttonGroup;
            AnchorOn.Group = _buttonGroup;

            // Send off a request to get the current dampening mode.
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnInertiaDampeningModeChanged?.Invoke(shuttle, InertiaDampeningMode.Query);

            ServiceFlagServices.OnPressed += _ => ToggleServiceFlags(ServiceFlags.Services);
            ServiceFlagTrade.OnPressed += _ => ToggleServiceFlags(ServiceFlags.Trade);
            ServiceFlagSocial.OnPressed += _ => ToggleServiceFlags(ServiceFlags.Social);

            TargetX.OnTextChanged += _ => _targetCoordsModified = true;
            TargetY.OnTextChanged += _ => _targetCoordsModified = true;
            TargetSet.OnPressed += _ => SetTargetCoords();
            TargetShow.OnPressed += _ => SetHideTarget(!TargetShow.Pressed);
        }

        private void SetDampenerMode(InertiaDampeningMode mode)
        {
            NavRadar.DampeningMode = mode;
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnInertiaDampeningModeChanged?.Invoke(shuttle, mode);
        }

        private void NfUpdateState(NavInterfaceState state)
        {
            if (NavRadar.DampeningMode == InertiaDampeningMode.Station)
            {
                DampenerModeButtons.Visible = false;
                ServiceFlagsBox.Visible = false;
            }
            else
            {
                DampenerModeButtons.Visible = true;
                ServiceFlagsBox.Visible = true;
                DampenerOff.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Off;
                DampenerOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Dampen;
                AnchorOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Anchor;
                ToggleServiceFlags(NavRadar.ServiceFlags, updateButtonsOnly: true);
            }

            TargetShow.Pressed = !state.HideTarget;
            if (!_targetCoordsModified)
            {
                if (state.Target != null)
                {
                    var target = state.Target.Value;
                    TargetX.Text = target.X.ToString("F1");
                    TargetY.Text = target.Y.ToString("F1");
                }
                else
                {
                    TargetX.Text = 0.0f.ToString("F1");
                    TargetY.Text = 0.0f.ToString("F1");
                }
            }
        }

        private void OnRangeFilterChanged(int value)
        {
            NavRadar.MaximumIFFDistance = value;
        }

        private void ToggleServiceFlags(ServiceFlags flags, bool updateButtonsOnly = false)
        {
            if (!updateButtonsOnly)
            {
                // Special handling for ServiceFlags.None
                if (flags == ServiceFlags.None)
                {
                    // If None is being toggled, set it to None (clear all other flags)
                    // No need to check if None is already set since that check will always be false
                    NavRadar.ServiceFlags = ServiceFlags.None;
                }
                else
                {
                    // Toggle the requested flag
                    NavRadar.ServiceFlags ^= flags;

                    // If any flag other than None is set, make sure None is unset
                    if (NavRadar.ServiceFlags != 0)
                    {
                        NavRadar.ServiceFlags &= ~ServiceFlags.None; // This is redundant since None is 0
                    }
                    // If toggling resulted in no flags, set None
                    else
                    {
                        NavRadar.ServiceFlags = ServiceFlags.None;
                    }
                }
                _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
                OnServiceFlagsChanged?.Invoke(shuttle, NavRadar.ServiceFlags);
            }

            ServiceFlagServices.Pressed = NavRadar.ServiceFlags.HasFlag(ServiceFlags.Services);
            ServiceFlagTrade.Pressed = NavRadar.ServiceFlags.HasFlag(ServiceFlags.Trade);
            ServiceFlagSocial.Pressed = NavRadar.ServiceFlags.HasFlag(ServiceFlags.Social);
        }

        private void NfAddShuttleDesignation(EntityUid? shuttle)
        {
            if (_entManager.TryGetComponent<MetaDataComponent>(shuttle, out var metadata))
            {
                var shipNameParts = metadata.EntityName.Split(' ');
                var designation = shipNameParts[^1];
                if (designation.Length > 2 && designation[2] == '-')
                {
                    NavDisplayLabel.Text = string.Join(' ', shipNameParts[..^1]);
                    ShuttleDesignation.Text = designation;
                }
                else
                    NavDisplayLabel.Text = metadata.EntityName;
            }
        }

        private void SetTargetCoords()
        {
            Vector2 outputVector;
            if (!float.TryParse(TargetX.Text, out outputVector.X))
                outputVector.X = 0.0f;

            if (!float.TryParse(TargetY.Text, out outputVector.Y))
                outputVector.Y = 0.0f;

            NavRadar.Target = outputVector;
            NavRadar.TargetEntity = NetEntity.Invalid;
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnSetTargetCoordinates?.Invoke(shuttle, outputVector);
            _targetCoordsModified = false;
        }

        private void SetHideTarget(bool hide)
        {
            _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
            OnSetHideTarget?.Invoke(shuttle, hide);
        }
    }
}

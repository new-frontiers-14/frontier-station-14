// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using System.Numerics;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared.Shuttles.Components;

namespace Content.Client.Shuttles.UI
{
    public sealed partial class ShuttleConsoleWindow
    {
        public event Action<NetEntity?, InertiaDampeningMode>? OnInertiaDampeningModeChanged;
        public event Action<NetEntity?, ServiceFlags>? OnServiceFlagsChanged;
        public event Action<NetEntity?, Vector2>? OnSetTargetCoordinates;
        public event Action<NetEntity?, bool>? OnSetHideTarget;

        private void NfInitialize()
        {
            NavContainer.OnInertiaDampeningModeChanged += (entity, mode) =>
            {
                OnInertiaDampeningModeChanged?.Invoke(entity, mode);
            };
            NavContainer.OnServiceFlagsChanged += (entity, flags) =>
            {
                OnServiceFlagsChanged?.Invoke(entity, flags);
            };
            NavContainer.OnSetTargetCoordinates += (entity, position) =>
            {
                OnSetTargetCoordinates?.Invoke(entity, position);
            };
            NavContainer.OnSetHideTarget += (entity, hide) =>
            {
                OnSetHideTarget?.Invoke(entity, hide);
            };
        }

    }
}

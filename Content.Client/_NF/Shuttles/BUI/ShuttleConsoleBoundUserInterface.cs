// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.
using Content.Client.Shuttles.UI;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared.Shuttles.Components;

namespace Content.Client.Shuttles.BUI
{
    public sealed partial class ShuttleConsoleBoundUserInterface
    {
        private void NfOpen()
        {
            _window ??= new ShuttleConsoleWindow();
            _window.OnInertiaDampeningModeChanged += OnInertiaDampeningModeChanged;
            _window.OnServiceFlagsChanged += OnServiceFlagsChanged;
        }
        private void OnInertiaDampeningModeChanged(NetEntity? entityUid, InertiaDampeningMode mode)
        {
            SendMessage(new SetInertiaDampeningRequest
            {
                ShuttleEntityUid = entityUid,
                Mode = mode,
            });
        }

        private void OnServiceFlagsChanged(NetEntity? entityUid, ServiceFlags flags)
        {
            SendMessage(new SetServiceFlagsRequest
            {
                ShuttleEntityUid = entityUid,
                ServiceFlags = flags,
            });
        }

    }
}

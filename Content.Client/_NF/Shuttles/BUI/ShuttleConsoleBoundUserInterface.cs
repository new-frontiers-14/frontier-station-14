
using Content.Client.Shuttles.UI;
using Content.Shared._NF.Shuttles.Events;

namespace Content.Client.Shuttles.BUI
{
    public sealed partial class ShuttleConsoleBoundUserInterface
    {
        private void NfOpen()
        {
            _window ??= new ShuttleConsoleWindow();
            _window.OnChangeInertiaDampeningTypeRequest += OnChangeInertiaDampeningTypeRequest;
        }
        private void OnChangeInertiaDampeningTypeRequest(NetEntity? entityUid, InertiaDampeningMode mode)
        {
            SendMessage(new ToggleStabilizerRequest
            {
                ShuttleEntityUid = entityUid,
                Mode = mode,
            });
        }

    }
}

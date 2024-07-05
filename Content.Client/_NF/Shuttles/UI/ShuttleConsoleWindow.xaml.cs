using Content.Shared._NF.Shuttles.Events;

namespace Content.Client.Shuttles.UI
{
    public sealed partial class ShuttleConsoleWindow
    {
        public event Action<NetEntity?, InertiaDampeningMode>? OnChangeInertiaDampeningTypeRequest;

        private void NfInitialize()
        {
            NavContainer.OnChangeInertiaDampeningTypeRequest += (entity, mode) =>
            {
                OnChangeInertiaDampeningTypeRequest?.Invoke(entity, mode);
            };
        }

    }
}

using Content.Shared._NF.M_Emp;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._NF.M_Emp.UI
{
    [UsedImplicitly]
    public sealed class M_EmpBoundUserInterface : BoundUserInterface
    {
        private M_EmpWindow? _window;

        public M_EmpBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new M_EmpWindow(this);
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedReagentDispenserComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

        //    var castState = (M_EmpBoundUserInterfaceState) state;
        //    _window?.UpdateState(castState); //Update window state
        }

        public void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}


using Content.Client.Chemistry.UI;
using Content.Shared._NF.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="ChemPrenticeWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemPrenticeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ChemPrenticeWindow? _window;

        public ChemPrenticeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = this.CreateWindow<ChemPrenticeWindow>();
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            // Setup static button actions.
            _window.InputEjectButton.OnPressed += _ => SendMessage(
                new ItemSlotButtonPressedEvent(SharedChemMaster.InputSlotName));
            //_window.OutputEjectButton.OnPressed += _ => SendMessage(
            //    new ItemSlotButtonPressedEvent(SharedChemPrentice.OutputSlotName));
            _window.BufferTransferButton.OnPressed += _ => SendMessage(
                new ChemMasterSetModeMessage(ChemMasterMode.Transfer));
            _window.BufferDiscardButton.OnPressed += _ => SendMessage(
                new ChemMasterSetModeMessage(ChemMasterMode.Discard));
            //_window.CreatePillButton.OnPressed += _ => SendMessage(
            //    new ChemPrenticeCreatePillsMessage(
            //        (uint) _window.PillDosage.Value, (uint) _window.PillNumber.Value, _window.LabelLine));
            //_window.CreateBottleButton.OnPressed += _ => SendMessage(
            //    new ChemPrenticeOutputToBottleMessage(
            //        (uint) _window.BottleDosage.Value, _window.LabelLine));

            /*for (uint i = 0; i < _window.PillTypeButtons.Length; i++)
            {
                var pillType = i;
                _window.PillTypeButtons[i].OnPressed += _ => SendMessage(new ChemPrenticeSetPillTypeMessage(pillType));
            }*/

            _window.OnReagentButtonPressed += (args, button) => SendMessage(new ChemMasterReagentAmountButtonMessage(button.Id, button.Amount, button.IsBuffer));
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

            var castState = (ChemPrenticeBoundUserInterfaceState)state;

            _window?.UpdateState(castState); // Update window state
        }
    }
}

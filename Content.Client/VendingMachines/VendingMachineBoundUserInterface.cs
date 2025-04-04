using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;
using Robust.Client.GameObjects;
using Content.Shared._NF.Bank.Components; // Frontier
using Content.Shared.Containers.ItemSlots; // Frontier
using Content.Shared.Stacks; // Frontier

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        // Frontier: market price modifier & balance
        private UserInterfaceSystem _uiSystem = default!;
        private ItemSlotsSystem _itemSlots = default!;

        [ViewVariables]
        private float _mod = 1f;
        [ViewVariables]
        private int _balance = 0;
        [ViewVariables]
        private int _cashSlotBalance = 0;
        // End Frontier

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            // Frontier: state, market modifier, balance status
            _uiSystem = EntMan.System<UserInterfaceSystem>();
            _itemSlots = EntMan.System<ItemSlotsSystem>();

            if (EntMan.TryGetComponent<MarketModifierComponent>(Owner, out var market))
                _mod = market.Mod;
            // End Frontier

            _menu = this.CreateWindowCenteredLeft<VendingMachineMenu>();
            // Frontier: no exceptions
            if (EntMan.TryGetComponent(Owner, out MetaDataComponent? meta))
                _menu.Title = meta.EntityName;
            else
                _menu.Title = Loc.GetString("vending-machine-nf-fallback-title");
            // End Frontier: no exceptions
            _menu.OnItemSelected += OnItemSelected;
            Refresh();
        }

        public void Refresh()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            // Frontier: state, market modifier, balance status
            if (EntMan.TryGetComponent<BankAccountComponent>(PlayerManager.LocalEntity, out var bank))
                _balance = bank.Balance;
            else
                _balance = 0;
            int? cashSlotValue = null;
            if (TryUpdateCashSlotBalance())
                cashSlotValue = _cashSlotBalance;
            // End Frontier

            _menu?.Populate(_cachedInventory, enabled, _mod, _balance, cashSlotValue); // Frontier: add _mod, _balance, cashSlotValue
        }

        public void UpdateAmounts()
        {
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            // Frontier: get bank balance
            if (EntMan.TryGetComponent<BankAccountComponent>(PlayerManager.LocalEntity, out var bank))
                _balance = bank.Balance;
            else
                _balance = 0;
            _menu?.UpdateBalance(_balance);
            if (TryUpdateCashSlotBalance())
                _menu?.UpdateCashSlotBalance(_cashSlotBalance);
            // End Frontier

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);
            _menu?.UpdateAmounts(_cachedInventory, _mod, enabled); // Frontier: add _mod
        }

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            if (data is not VendorItemsListData { ItemIndex: var itemIndex })
                return;

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(itemIndex);

            if (selectedItem == null)
                return;

            SendPredictedMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemSelected -= OnItemSelected;
            _menu.OnClose -= Close;
            _menu.Dispose();
        }

        // Frontier: update cash slot balance
        public bool TryUpdateCashSlotBalance()
        {
            if (EntMan.TryGetComponent<VendingMachineComponent>(Owner, out var vendingMachine))
            {
                _cashSlotBalance = vendingMachine.CashSlotBalance;
                return true;
            }
            else
            {
                _cashSlotBalance = 0;
                return false;
            }
        }
        // End Frontier
    }
}

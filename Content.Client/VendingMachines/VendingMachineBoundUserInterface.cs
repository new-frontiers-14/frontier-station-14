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

            _menu = this.CreateWindow<VendingMachineMenu>();
            _menu.OpenCenteredLeft();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            _menu.OnItemSelected += OnItemSelected;
            Refresh();
        }

        public void Refresh()
        {
            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            // Frontier: state, market modifier, balance status
            var uiUsers = _uiSystem.GetActors(Owner, UiKey);
            foreach (var uiUser in uiUsers)
            {
                if (EntMan.TryGetComponent<BankAccountComponent>(uiUser, out var bank))
                    _balance = bank.Balance;
            }
            int? cashSlotValue = null;
            if (EntMan.TryGetComponent<VendingMachineComponent>(Owner, out var vendingMachine))
            {
                _cashSlotBalance = vendingMachine.CashSlotBalance;
                if (vendingMachine.CashSlotName != null)
                    cashSlotValue = _cashSlotBalance;
            }
            else
            {
                _cashSlotBalance = 0;
            }
            // End Frontier

            _menu?.Populate(_cachedInventory, _mod, _balance, cashSlotValue); // Frontier: add _balance
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

            SendMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
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
    }
}

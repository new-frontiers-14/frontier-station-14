using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Content.Shared.Bank.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;
using Robust.Client.GameObjects;

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
        private IEntityManager _entMan = default!;

        [ViewVariables]
        private float _mod = 1f;
        [ViewVariables]
        private int _balance = 0;
        // End Frontier

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();

            // Frontier: state, market modifier, balance status
            _entMan = entMan;
            _uiSystem = entMan.System<UserInterfaceSystem>();

            if (entMan.TryGetComponent<MarketModifierComponent>(Owner, out var market))
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
                if (_entMan.TryGetComponent<BankAccountComponent>(uiUser, out var bank))
                    _balance = bank.Balance;
            }
            // End Frontier

            _menu?.Populate(_cachedInventory, _mod, _balance); // Frontier: add _balance
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

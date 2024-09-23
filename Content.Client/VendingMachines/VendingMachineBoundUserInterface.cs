using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
<<<<<<< HEAD
using Content.Shared.Bank.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using System.Linq;
=======
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

<<<<<<< HEAD
        [ViewVariables]
        private List<int> _cachedFilteredIndex = new();

        [ViewVariables]
        private float _mod = 1f;

=======
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

<<<<<<< HEAD
            var entMan = IoCManager.Resolve<IEntityManager>();
            var vendingMachineSys = entMan.System<VendingMachineSystem>();

            if (entMan.TryGetComponent<MarketModifierComponent>(Owner, out var market))
                _mod = market.Mod;

            _cachedInventory = vendingMachineSys.GetAllInventory(Owner);

=======
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
            _menu = this.CreateWindow<VendingMachineMenu>();
            _menu.OpenCenteredLeft();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            _menu.OnItemSelected += OnItemSelected;
<<<<<<< HEAD
            _menu.OnSearchChanged += OnSearchChanged;

            _menu.Populate(_cachedInventory, _mod, out _cachedFilteredIndex); // Frontier: add _mod
=======
            Refresh();
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
        }

        public void Refresh()
        {
            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(_cachedInventory);
        }

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

<<<<<<< HEAD
            var entMan = IoCManager.Resolve<IEntityManager>();
            var priceMod = 1f;

            if (entMan.TryGetComponent<MarketModifierComponent>(Owner, out var market))
                priceMod = market.Mod;

            _cachedInventory = newState.Inventory;
            _menu?.UpdateBalance(newState.Balance);
            _menu?.Populate(_cachedInventory, priceMod, out _cachedFilteredIndex, _menu.SearchBar.Text);
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
=======
            if (data is not VendorItemsListData { ItemIndex: var itemIndex })
                return;

>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
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
<<<<<<< HEAD

        private void OnSearchChanged(string? filter)
        {
            _menu?.Populate(_cachedInventory, _mod, out _cachedFilteredIndex, filter);
        }
=======
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
    }
}

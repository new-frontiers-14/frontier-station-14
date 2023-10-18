using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Content.Shared.Bank.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using System.Linq;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        [ViewVariables]
        private float _mod = 1f;

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();
            var vendingMachineSys = entMan.System<VendingMachineSystem>();

            if (entMan.TryGetComponent<MarketModifierComponent>(Owner, out var market))
            {
                _mod = market.Mod;
            }

            _cachedInventory = vendingMachineSys.GetAllInventory(Owner);

            _menu = new VendingMachineMenu { Title = entMan.GetComponent<MetaDataComponent>(Owner).EntityName };

            _menu.OnClose += Close;
            _menu.OnItemSelected += OnItemSelected;
            _menu.OnSearchChanged += OnSearchChanged;

            _menu.Populate(_cachedInventory, _mod);

            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not VendingMachineInterfaceState newState)
                return;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var priceMod = 1f;

            if (entMan.TryGetComponent<MarketModifierComponent>(Owner, out var market))
            {
                priceMod = market.Mod;
            }
            _cachedInventory = newState.Inventory;
            _menu?.UpdateBalance(newState.Balance);
            _menu?.Populate(_cachedInventory, priceMod);
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(args.ItemIndex);

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

        private void OnSearchChanged(string? filter)
        {
            _menu?.Populate(_cachedInventory, _mod, filter);
        }
    }
}

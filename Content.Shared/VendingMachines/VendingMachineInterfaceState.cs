using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines
{
<<<<<<< HEAD
    [NetSerializable, Serializable]
    public sealed class VendingMachineInterfaceState : BoundUserInterfaceState
    {
        public List<VendingMachineInventoryEntry> Inventory;
        public int Balance;

        public VendingMachineInterfaceState(List<VendingMachineInventoryEntry> inventory, int balance)
        {
            Inventory = inventory;
            Balance = balance;
        }
    }

=======
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
    [Serializable, NetSerializable]
    public sealed class VendingMachineEjectMessage : BoundUserInterfaceMessage
    {
        public readonly InventoryType Type;
        public readonly string ID;
        public VendingMachineEjectMessage(InventoryType type, string id)
        {
            Type = type;
            ID = id;
        }
    }

    [Serializable, NetSerializable]
    public enum VendingMachineUiKey
    {
        Key,
    }
}

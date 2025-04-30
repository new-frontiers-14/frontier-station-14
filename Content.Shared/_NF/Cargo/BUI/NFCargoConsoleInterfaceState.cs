using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.BUI;

[Serializable, NetSerializable]
public sealed class CargoConsoleInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public int Count;
    public int Capacity;
    public int Balance;
    public List<NFCargoOrderData> Orders;

    public CargoConsoleInterfaceState(string name, int count, int capacity, int balance, List<NFCargoOrderData> orders)
    {
        Name = name;
        Count = count;
        Capacity = capacity;
        Balance = balance;
        Orders = orders;
    }
}

using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.BUI;

[Serializable, NetSerializable]
public sealed class NFCargoConsoleInterfaceState(
    string name,
    int count,
    int capacity,
    int balance,
    List<NFCargoOrderData> orders) : BoundUserInterfaceState
{
    public string Name = name;
    public int Count = count;
    public int Capacity = capacity;
    public int Balance = balance;
    public List<NFCargoOrderData> Orders = orders;
}

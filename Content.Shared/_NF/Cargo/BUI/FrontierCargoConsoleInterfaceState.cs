namespace Content.Shared._NF.Cargo.BUI;

using Robust.Shared.Serialization;

[Serializable, NetSerializable]
public sealed class FrontierCargoConsoleInterfaceState(
    string name,
    int count,
    int capacity,
    int balance,
    List<FrontierCargoOrderData> orders) : BoundUserInterfaceState
{
    public string Name = name;
    public int Count = count;
    public int Capacity = capacity;
    public int Balance = balance;
    public List<FrontierCargoOrderData> Orders = orders;
}

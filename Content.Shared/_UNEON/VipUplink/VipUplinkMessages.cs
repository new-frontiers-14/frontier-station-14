using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._UNEON.VipUplink;

[Serializable, NetSerializable]
public sealed class VipUplinkBuyMessage : CartridgeMessageEvent
{
    public readonly string ProductId;
    public VipUplinkBuyMessage(string productId) { ProductId = productId; }
}

[Serializable, NetSerializable]
public sealed class VipUplinkState : BoundUserInterfaceState
{
    public readonly bool Allowed;
    public readonly int Balance;
    public readonly List<VipUplinkListing> Products;
    public VipUplinkState(bool allowed, int balance, List<VipUplinkListing> products)
    { Allowed = allowed; Balance = balance; Products = products; }
}

[Serializable, NetSerializable]
public sealed class VipUplinkListing
{
    public readonly string Id;
    public readonly string Name;
    public readonly int Price;
    public VipUplinkListing(string id, string name, int price) { Id = id; Name = name; Price = price; }
}

/// <summary>Personal catalogue and balance are sent only to the PDA viewer concerned, never broadcast.</summary>
[Serializable, NetSerializable]
public sealed class VipUplinkStateMessage : BoundUserInterfaceMessage
{
    public readonly VipUplinkState State;
    public VipUplinkStateMessage(VipUplinkState state) { State = state; }
}

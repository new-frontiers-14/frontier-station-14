using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market.Events;

/// <summary>
/// Message to move an item between cart and market
/// </summary>
[Serializable, NetSerializable]
public sealed class MarketConsoleCartMessage : BoundUserInterfaceMessage
{
    public int Amount;
    public string? ItemPrototype;

    public MarketConsoleCartMessage(int amount, string itemPrototype)
    {
        Amount = amount;
        ItemPrototype = itemPrototype;
    }
}


using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market.Events;

/// <summary>
/// Raised on a client request pallet sale
/// </summary>
[Serializable, NetSerializable]
public sealed class MarketPalletSellMessage : BoundUserInterfaceMessage
{

}

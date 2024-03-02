using Robust.Shared.Serialization;

namespace Content.Shared._NF.Contraband.Events;

/// <summary>
/// Raised on a client request pallet sale
/// </summary>
[Serializable, NetSerializable]
public sealed class ContrabandPalletSellMessage : BoundUserInterfaceMessage
{

}

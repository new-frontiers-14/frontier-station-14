using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market.Events;

/// <summary>
///     Purchase a crate message.
/// </summary>
[Serializable, NetSerializable]
public sealed class CrateMachinePurchaseMessage : BoundUserInterfaceMessage
{

}


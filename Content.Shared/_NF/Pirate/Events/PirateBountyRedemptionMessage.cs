using Robust.Shared.Serialization;

namespace Content.Shared._NF.Pirate.Events;

/// <summary>
/// Raised on a client request pallet sale
/// </summary>
[Serializable, NetSerializable]
public sealed class PirateBountyRedemptionMessage : BoundUserInterfaceMessage
{
    public PirateBountyRedemptionMessage()
    {
    }
}

using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.Events;

/// <summary>
///     Set order in database as approved.
/// </summary>
[Serializable, NetSerializable]
public sealed class FrontierCargoConsoleApproveOrderMessage(int orderId) : BoundUserInterfaceMessage
{
    public int OrderId = orderId;
}


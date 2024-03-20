using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.Events;

/// <summary>
///     Remove order from database.
/// </summary>
[Serializable, NetSerializable]
public sealed class FrontierCargoConsoleRemoveOrderMessage(int orderId) : BoundUserInterfaceMessage
{
    public int OrderId = orderId;
}


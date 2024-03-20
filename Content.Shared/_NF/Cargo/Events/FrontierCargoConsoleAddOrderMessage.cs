using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.Events;

/// <summary>
///     Add order to database.
/// </summary>
[Serializable, NetSerializable]
public sealed class FrontierCargoConsoleAddOrderMessage(string requester, string reason, string cargoProductId, int amount) : BoundUserInterfaceMessage
{
    public string Requester = requester;
    public string Reason = reason;
    public string CargoProductId = cargoProductId;
    public int Amount = amount;
}

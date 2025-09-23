using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.CryoSleep;

/// <summary>
/// A message for CryoSleepEui containing all the items the server found, along with some other data to build the clientside warning messages.
/// </summary>
[Serializable] [NetSerializable]
public sealed class CryoSleepWarningMessage(
    bool shuttleOnPda,
    CryoSleepWarningMessage.NetworkedWarningItem? inventoryShuttleDeed,
    bool foundMoreShuttles,
    CryoSleepWarningMessage.NetworkedWarningItem? foundUplink,
    List<CryoSleepWarningMessage.NetworkedWarningItem> importantItems)
    : EuiMessageBase
{
    public readonly bool ShuttleOnPDA = shuttleOnPda;
    public readonly NetworkedWarningItem? InventoryShuttleDeed = inventoryShuttleDeed;
    public readonly bool FoundMoreShuttles = foundMoreShuttles;
    public readonly NetworkedWarningItem? FoundUplink = foundUplink;
    public readonly List<NetworkedWarningItem> ImportantItems = importantItems;

    [Serializable] [NetSerializable]
    public struct NetworkedWarningItem
    {
    public NetworkedWarningItem(string? slotId, NetEntity? container, NetEntity item)
    {
        if (slotId == null && !container.HasValue)
        {
            throw new ArgumentException(
                "CryoSleepWarningMessage.NetworkedWarningItem was attempted to be created with both slotId and container as null values");
        }

        SlotId = slotId;
        Container = container;
        Item = item;
        }
 //Exactly one of these two values should be null
        public readonly string? SlotId;
        public readonly NetEntity? Container;

        public readonly NetEntity Item;
    }
}

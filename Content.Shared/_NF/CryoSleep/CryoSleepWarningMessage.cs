using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.CryoSleep;

[Serializable] [NetSerializable]
public sealed class CryoSleepWarningMessage(
    bool shuttleOnPda,
    CryoSleepWarningMessage.NetworkedWarningItem? inventoryShuttleDeed,
    CryoSleepWarningMessage.NetworkedWarningItem? foundUplink,
    List<CryoSleepWarningMessage.NetworkedWarningItem> importantItems)
    : EuiMessageBase
{
    public readonly bool ShuttleOnPDA = shuttleOnPda;
    public readonly NetworkedWarningItem? InventoryShuttleDeed = inventoryShuttleDeed;
    public readonly NetworkedWarningItem? FoundUplink = foundUplink;
    public readonly List<NetworkedWarningItem> ImportantItems = importantItems;

    [Serializable] [NetSerializable]
    public struct NetworkedWarningItem(string? slotId, NetEntity? container, NetEntity item)
    {
        //Exactly one of these two values should be null
        public readonly string? SlotId = slotId;
        public readonly NetEntity? Container = container;

        public readonly NetEntity Item = item;
    }
}

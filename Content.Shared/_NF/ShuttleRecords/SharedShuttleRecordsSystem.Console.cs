using Content.Shared._NF.ShuttleRecords.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords;

public sealed partial class ShuttleRecordsSystem : SharedShuttleRecordsSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShuttleRecordsConsoleComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, ShuttleRecordsConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, ShuttleRecordsConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
    }
}

[NetSerializable, Serializable]
public enum ShuttleRecordsUiKey : byte
{
    Default,
}

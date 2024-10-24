using Content.Shared._NF.ShuttleRecords.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords;

public abstract class SharedShuttleRecordsSystem : EntitySystem
{
    // These dependencies are eventually needed for the consoles that are made for this system.
    [Dependency] protected readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] protected readonly SharedAudioSystem _audioSystem = default!;

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

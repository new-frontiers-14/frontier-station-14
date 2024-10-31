using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._NF.ShuttleRecords;

public abstract class SharedShuttleRecordsSystem : EntitySystem
{
    // These dependencies are eventually needed for the consoles that are made for this system.
    [Dependency] protected readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] protected readonly SharedAudioSystem _audioSystem = default!;
}

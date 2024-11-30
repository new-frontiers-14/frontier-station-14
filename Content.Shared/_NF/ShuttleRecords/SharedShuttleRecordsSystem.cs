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

    /// <summary>
    /// Get the transaction cost for the given shipyard and sell value.
    /// </summary>
    /// <param name="percent">The percentage of the shuttle to use as a base for the cost</param>
    /// <param name="min">The maximum price for a deed copy</param>
    /// <param name="max">The minimum price for a deed copy</param>
    /// <param name="fixedPrice">Optionally, the fixed price for a deed copy</param>
    /// <param name="vesselPrice">The cost to purchase the ship</param>
    /// <returns>The transaction cost for this ship.</returns>
    public static uint GetTransactionCost(double percent, uint min, uint max, uint vesselPrice, uint? fixedPrice)
    {
        var cost = fixedPrice ?? (uint)(vesselPrice * percent);
        return Math.Clamp(cost, min, max);
    }
}

[NetSerializable, Serializable]
public enum ShuttleRecordsUiKey : byte
{
    Default,
}

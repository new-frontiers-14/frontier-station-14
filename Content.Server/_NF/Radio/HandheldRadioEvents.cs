using Content.Server.Chat.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Server._NF.Radio;

/// <summary>
///     This event will be broadcast right before displaying an entities typing indicator.
///     If _overrideIndicator is not null after the event is finished it will be used.
/// </summary>
[ByRefEvent]
public sealed class SpeakHandheldRadioEvent : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.All;

    public required EntitySpokeEvent SpeakEvent;
}

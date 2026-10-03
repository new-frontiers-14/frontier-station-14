using Content.Server.Chat.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Server._NF.Radio;

/// <summary>
///     Event relayed to hands + inventory when an entity speaks
///     Used for the handheld radio to listen to the player speaking
/// </summary>
[ByRefEvent]
public sealed class SpeakHandheldRadioEvent : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.All;

    public required EntitySpokeEvent SpeakEvent;
}

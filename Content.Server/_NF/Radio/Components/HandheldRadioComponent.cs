using Content.Server._NF.Radio.Systems;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Radio.Components;

/// <summary>
///     Replacement for NC's handheld radio component.
///     Handles both sending/receiving messages.
/// </summary>
[RegisterComponent]
[Access(typeof(HandheldRadioSystem))]
public sealed partial class HandheldRadioComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("channel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string Channel = SharedChatSystem.CommonChannel;

    /// <summary>
    // The radio frequency on which the message will be transmitted
    /// </summary>
    [DataField]
    public int Frequency = 1459; // Common channel frequency
}

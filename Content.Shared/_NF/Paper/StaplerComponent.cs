using Robust.Shared.Audio;

namespace Content.Shared._NF.Paper;

[RegisterComponent]
[Access(typeof(StaplerSystem))]
public sealed partial class StaplerComponent : Component
{
    /// <summary>
    /// The ID of the item slot used to hold a paper in the stapler.
    /// </summary>
    [DataField, ViewVariables]
    public string SlotId = "stapler_slot";

    /// <summary>
    /// Sound played when stapling papers together.
    /// </summary>
    [DataField]
    public SoundSpecifier StapleSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg");
}

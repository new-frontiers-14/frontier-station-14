using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Paper;

[RegisterComponent, NetworkedComponent]
[Access(typeof(StaplerSystem))]
public sealed partial class StaplerComponent : Component
{
    /// <summary>
    /// The ID of the item slot used to hold a paper in the stapler.
    /// </summary>
    [DataField]
    public string SlotId = "stapler_slot";

    /// <summary>
    /// Sound played when stapling papers together.
    /// </summary>
    [DataField]
    public SoundSpecifier StapleSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg");

    /// <summary>
    /// The prototype ID to spawn when creating a new paper bundle.
    /// </summary>
    [DataField]
    public EntProtoId BundlePrototype = "PaperBundle";
}

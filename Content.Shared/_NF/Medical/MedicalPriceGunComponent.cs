using Robust.Shared.Audio;

namespace Content.Server._NF.Medical.Components;

[RegisterComponent]
public sealed partial class MedicalPriceGunComponent : Component
{
    /// <summary>
    /// The sound that plays when the price gun appraises an object.
    /// </summary>
    [DataField]
    public SoundSpecifier AppraisalSound = new SoundPathSpecifier("/Audio/Items/appraiser.ogg");
}

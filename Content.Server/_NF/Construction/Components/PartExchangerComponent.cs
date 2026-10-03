using Robust.Shared.Audio;

namespace Content.Server._NF.Construction.Components;

[RegisterComponent]
public sealed partial class PartExchangerComponent : Component
{
    /// <summary>
    /// How long it takes to exchange the parts
    /// </summary>
    [DataField("exchangeDuration")]
    public float ExchangeDuration = 3;

    /// <summary>
    /// Whether or not the distance check is needed.
    /// Good for BRPED.
    /// </summary>
    /// <remarks>
    /// I fucking hate BRPED and if you ever add it
    /// i will personally kill your dog.
    /// </remarks>
    [DataField("doDistanceCheck")]
    public bool DoDistanceCheck = true;

    [DataField("exchangeSound")]
    public SoundSpecifier ExchangeSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    /// <summary>
    /// Whether the exchanger should install the highest-rated available parts instead of the lowest-rated ones.
    /// </summary>
    /// <remarks>
    /// If the BRPED is what I think it is
    /// jokes on you I don't have a dog. :P
    /// </remarks>
    [DataField("preferHigherRating")]
    public bool PreferHigherRating = true;

    public EntityUid? AudioStream;
}

using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Security.Components;

/// <summary>
/// This is used for the contraband appraisal gun, which checks the contraband turn-in value in FUCs of any object it appraises.
/// </summary>
[RegisterComponent]
public sealed partial class ContrabandPriceGunComponent : Component
{
    /// <summary>
    /// The currency that scanned items will be checked for.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string Currency = "FrontierUplinkCoin";

    /// <summary>
    /// The prefix for localization strings to display.
    /// </summary>
    [DataField]
    public string LocStringPrefix = string.Empty;

    /// <summary>
    /// The sound that plays when the price gun appraises an object.
    /// </summary>
    [DataField]
    public SoundSpecifier AppraisalSound = new SoundPathSpecifier("/Audio/Items/appraiser.ogg");
}

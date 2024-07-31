using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Security.Components;

/// <summary>
/// This is used for the contraband appraisal gun, which checks the contraband turn-in value in FUCs of any object it appraises.
/// </summary>
[RegisterComponent]
public sealed partial class ContrabandPriceGunComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string Currency = "FrontierUplinkCoin";
    [DataField]
    public string LocStringPrefix = string.Empty;
}

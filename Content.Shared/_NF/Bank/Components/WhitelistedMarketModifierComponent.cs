using Content.Shared.Whitelist;

namespace Content.Shared._NF.Bank.Components;

/// <summary>
/// This is used to apply a price modifier to items that pass a whitelist/blacklist.
/// </summary>
[RegisterComponent]
public sealed partial class WhitelistedMarketModifierComponent : Component
{
    /// <summary>
    /// The amount to multiply matching item's price by
    /// </summary>
    [DataField(required: true)]
    public float Mod { get; set; } = 1.0f;

    /// <summary>
    /// Whitelist of items that should have a special modifier applied to them.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Description of which items this entity is providing a multiplier to.
    /// Used for examine text.
    /// </summary>
    [DataField]
    public LocId? Description;

    /// <summary>
    /// Whether an item receiving a modifier from this comp should skip receiving a modifier from <see cref="MarketModifierComponent"/>.
    /// E.g. if this comp grants a 2x modifier to entities with the "metal" tag and this datafield is true,
    /// then entities with that tag will be worth 2x even if a market modifier makes everything worth 0.5x.
    /// If false, then this modifier will be applied first and the market modifier will be applied on top of it.
    /// </summary>
    [DataField]
    public bool OverwriteMarketModifiers = true;
}

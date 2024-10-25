using Content.Server.Nutrition.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reagent;

[Prototype("digestion")]
[DataDefinition]
public sealed partial class DigestionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The list of digestive effects that occur in this organ.
    /// </summary>
    [DataField]
    public List<DigestionEffect> Effects { get; private set; } = new();

    /// <summary>
    /// The list of digestive effects that occur in this organ.
    /// </summary>
    [DataField]
    public ProtoId<DigestionPrototype>? PostDigest { get; private set; } = null;
}

[DataDefinition]
public sealed partial class DigestionEffect
{
    /// <summary>
    /// The quality of food this effect occurs on.
    /// </summary>
    [DataField]
    public FoodQuality? Quality { get; private set; } = null;

    /// <summary>
    /// A whitelist on the food item this bite was taken from.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist { get; private set; } = null;

    /// <summary>
    /// A list of reagent effects that happen on the stomach solution
    /// </summary>
    [DataField]
    public List<EntityEffect> Effects { get; private set; } = new();

    /// <summary>
    /// A list of conversions.  All values should be ratios of the input reagent, and the sum of their values should be <= 1.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>> Conversions { get; private set; } = new();
}

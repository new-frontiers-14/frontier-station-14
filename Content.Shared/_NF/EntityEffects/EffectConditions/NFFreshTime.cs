using Content.Shared.Atmos.Rotting;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

/// <summary>
/// Returns true if this entity is perishable and if its remaining fresh time is within a specified minimum and maximum.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class NFFreshTime : EntityEffectCondition
{
    [DataField("threshold", required: true)]
    public FreshTimeCondition Thresholds = default!;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out PerishableComponent? perishable))
        {
            var freshTimeLeft = perishable.RotAfter - perishable.RotAccumulator;
            return freshTimeLeft >= Thresholds.MinFreshTimeLeft && freshTimeLeft <= Thresholds.MaxFreshTimeLeft;
        }
        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        // FIXME: I'm not sure how to get fluent localization strings to accept TimeStamp arguments correctly
        return "Explanation for NFFreshTime not yet implemented";
        // Loc.GetString("reagent-effect-condition-guidebook-total-damage",
        //     ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
        //     ("min", Min.Float()));
    }
}

// This is ripped from a version that uses the upstream effectconditions, hence the name
/// <inheritdoc cref="EntityCondition"/>
[DataDefinition]
public partial record FreshTimeCondition
{
    [DataField("maxFreshTimeLeft")]
    public TimeSpan MaxFreshTimeLeft = TimeSpan.MaxValue;

    [DataField("minFreshTimeLeft")]
    public TimeSpan MinFreshTimeLeft = TimeSpan.MinValue;
}

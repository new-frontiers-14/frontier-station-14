using Content.Shared.EntityEffects;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

public sealed partial class NFSpecificDamage : EntityEffectCondition
{
    // If any threshold is satisfied, this condition evaluates TRUE. Otherwise, it evaluates as FALSE.

    /// <summary>
    /// Table of damage types and corresponding min/max values for this condition to be satisfied
    /// </summary>
    [DataField("typeThresholds")]
    public Dictionary<ProtoId<DamageTypePrototype>, SpecificDamageThresholds>? TypeThresholds = default!;
    /// <summary>
    /// Table of damage groups and corresponding min/max values for this condition to be satisfied
    /// </summary>
    [DataField("groupThresholds")]
    public Dictionary<ProtoId<DamageGroupPrototype>, SpecificDamageThresholds>? GroupThresholds = default!;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out DamageableComponent? damage))
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            if (TypeThresholds != null)
            {
                foreach (var (type, thresholds) in TypeThresholds)
                {
                    var specificdamage = damage.Damage.DamageDict.GetValueOrDefault(type);
                    if (specificdamage >= thresholds.Min && specificdamage <= thresholds.Max)
                    {
                        return true;
                    }
                }
            }
            if (GroupThresholds != null)
            {
                foreach (var (group, thresholds) in GroupThresholds)
                {
                    var groupProto = protoMan.Index(group);
                    var groupDamage = new Dictionary<string, FixedPoint2>();
                    foreach (var damageId in groupProto.DamageTypes)
                    {
                        var damageAmount = damage.Damage.DamageDict.GetValueOrDefault(damageId);
                        if (damageAmount != FixedPoint2.Zero)
                            groupDamage.Add(damageId, damageAmount);
                    }

                    var sum = groupDamage.Values.Sum();
                    if (sum >= thresholds.Min && sum <= thresholds.Max)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        // FIXME: Not sure how to specify the localization string for each combination of damage types
        // And above/below threshold conditions
        // Is it possible to set up a variadic name?
        return "Explanation for NFSpecificDamage not yet implemented";
        // Loc.GetString("reagent-effect-condition-guidebook-total-damage",
        //     ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
        //     ("min", Min.Float()));
    }

    [DataDefinition]
    public partial record SpecificDamageThresholds
    {
        [DataField("min")]
        public FixedPoint2 Min = FixedPoint2.Zero;
        [DataField("max")]
        public FixedPoint2 Max = FixedPoint2.MaxValue;
    }
}

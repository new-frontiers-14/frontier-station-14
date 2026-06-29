using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._NF.Addiction;

[Prototype, DataDefinition]
public sealed partial class AddictionPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AddictionPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> DefaultReagent { get; private set; }

    /// <summary>
    /// How long of a period in must the entity not get this AddictionEffect for its 'high' to drop. Defaults to 60 seconds
    /// </summary>
    [ViewVariables, DataField]
    public TimeSpan CheckPeriod { get; set; } = TimeSpan.FromMinutes(0.5);

    /// <summary>
    /// At what 'high' does the entity start to have an addiction
    /// </summary>
    [ViewVariables, DataField(required: true)]
    public FixedPoint2 Threshold { get; set; }

    /// <summary>
    /// How much to multiply the high rating by when the check period happens, should be less than 1 and greater than or equal to 0
    /// </summary>
    [ViewVariables, DataField(required: true)]
    public float DecayRate { get; set; }

    /// <summary>
    /// The maximum addiction rating this addiction can ever get. Null by default meaning there is no maximum
    /// </summary>
    [ViewVariables, DataField]
    public FixedPoint2? Max { get; set; }

    [DataField(required: true)]
    public WithdrawalData Withdrawal { get; private set; } = default!;


}

[DataDefinition]
public sealed partial class WithdrawalData
{
    /// <summary>
    /// How fast/slow the addiction rating decreases when the high is at 0 per check period. Must be less than 1 and greater than or equal to 0
    /// </summary>
    [DataField]
    public float DecayRate { get; private set; } = 0.5f;

    [DataField]
    public float Probability { get; private set; } = 1f;

    /// <summary>
    /// How fast or slow the addiction increases above the threshold. This is multiplied by the excess addiction factor. Effectively sets addiction to a percentage of the 'high'
    /// </summary>
    [ViewVariables, DataField]
    public float Multiplier { get; set; } = 1f;

    [DataField(required: true)]
    public SymptomEntry[] Symptoms { get; private set; }
}

[DataDefinition]
public sealed partial class SymptomEntry
{
    /// <summary>
    /// The minimum amount of withdrawal rating for this entry to apply
    /// </summary>
    [DataField]
    public FixedPoint2 Min { get; private set; } = 0;

    [DataField]
    public FixedPoint2 Max { get; private set; } = FixedPoint2.MaxValue;

    /// <summary>
    /// The amount of withdrawal rating this entries 'uses up'
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Rating { get; private set; }

    /// <summary>
    /// Can these effects be applied multiple times per withdrawal tick (useful for scaling hallucinations, drowsiness and similar effects that can stack)
    /// </summary>
    [DataField]
    public bool Repeatable { get; private set; } = false;

    /// <summary>
    /// If this entry is used how many seconds before another withdrawal effect should occur, cumulative for each entry chosen and number of times chosen
    /// </summary>
    [DataField]
    public TimeSpan Duration { get; private set; } = TimeSpan.FromSeconds(5);

    [DataField]
    public EntityEffectCondition[]? Conditions { get; private set; }

    [DataField]
    public float Probability = 1.0f;

    /// <summary>
    /// List of effects to perform on the entity if this withdrawal entry is chosen
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = default!;

}

public static class SymptomEntryExt
{
    public static bool ShouldApply(this SymptomEntry symptom, EntityEffectWithdrawalArgs args,
        IRobustRandom? random = null)
    {
        random ??= IoCManager.Resolve<IRobustRandom>();

        if (symptom.Min > args.Withdrawal || symptom.Max < args.Withdrawal)
            return false;

        if (symptom.Probability < 1.0f && !random.Prob(symptom.Probability))
            return false;

        if (symptom.Conditions != null)
        {
            foreach (var cond in symptom.Conditions)
            {
                if (!cond.Condition(args))
                    return false;
            }
        }

        return true;
    }
}

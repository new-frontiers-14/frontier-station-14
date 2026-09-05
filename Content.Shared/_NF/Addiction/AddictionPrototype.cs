using Content.Shared.Chemistry.Reagent;
using Content.Shared.Dataset;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
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
    public LocId Name;

    public string LocalizedName => Loc.GetString(Name);

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> DefaultReagent { get; private set; }

    /// <summary>
    /// How long of a period in must the entity not get this AddictionEffect for its 'high' to drop. Defaults to 30 seconds
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

    /// <summary>
    /// How fast or slow the addiction increases above the threshold. This is multiplied by the addiction factor. Effectively sets addiction to a percentage of the 'high'
    /// </summary>
    [ViewVariables, DataField]
    public float Multiplier { get; set; } = 1f;

    /// <summary>
    /// When there is no withdrawal, or no symptoms that satisfy their conditions this is how long before checking again
    /// </summary>
    public TimeSpan MinCheckDelay { get; private set; } = TimeSpan.FromSeconds(3);

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

    /// <summary>
    /// Maximum withdrawal rating for this entry to apply
    /// </summary>
    [DataField]
    public FixedPoint2 Max { get; private set; } = FixedPoint2.MaxValue;

    /// <summary>
    /// Minimum amount of time since entity had a high increased for the addiction
    /// </summary>
    [DataField]
    public TimeSpan MinTimeSinceHit { get; private set; } = TimeSpan.Zero;

    /// <summary>
    /// Maximum amount of time since entity had a high increased for the addiction
    /// </summary>
    [DataField]
    public TimeSpan MaxTimeSinceHit { get; private set; } = TimeSpan.MaxValue;

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
    public SymptomMessageList? Messages = null;

    [DataField]
    public float Probability = 1.0f;

    /// <summary>
    /// Optional List of effects to perform on the entity if this withdrawal entry is chosen, typically not specified if only messages are used
    /// </summary>
    [DataField]
    public EntityEffect[]? Effects { get; private set; } = default!;

}

[DataDefinition]
public sealed partial class SymptomMessageList
{
    /// <summary>
    /// Message from the list with the highest priority is displayed
    /// </summary>
    [DataField]
    public int Priority = 1;
    /// <summary>
    /// Probability of even showing this message, will fall back to lower priority messages of other symptoms if available
    /// </summary>
    [DataField]
    public float Probability = 1.0f;

    [DataField]
    public PopupRecipients Type = PopupRecipients.Local;

    [DataField]
    public PopupType VisualType = PopupType.Medium;

    //one of these next 2 should be defined
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? DataSet = null;
    [DataField]
    public LocId[]? List = null;
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

        if (symptom.MinTimeSinceHit > args.TimeSinceHit || symptom.MaxTimeSinceHit < args.TimeSinceHit)
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

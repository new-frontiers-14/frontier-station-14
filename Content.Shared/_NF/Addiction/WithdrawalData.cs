using Content.Shared.EntityEffects;

namespace Content.Shared._NF.Addiction;

[DataDefinition]
public sealed partial class WithdrawalData
{

    /// <summary>
    /// The maximum addiction rating this addiction can ever get. Null by default meaning there is no maximum
    /// </summary>
    [ViewVariables, DataField]
    public int? Max { get; set; }

    /// <summary>
    /// How fast/slow the addiction rating decreases when the high is at 0 per check period. Must be less than 1 and greater than or equal to 0
    /// </summary>
    [DataField]
    public float DecayRate { get; private set; } = 0.5f;

    [DataField]
    public float Probability { get; private set; } = 1f;

    /// <summary>
    /// How fast or slow the addiction increases above the threshold. This is multiplied by the excess addiction factor
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
    [DataField(required: true)]
    public int Min { get; private set; }

    [DataField]
    public int Max { get; private set; } = int.MaxValue;

    /// <summary>
    /// The amount of withdrawal rating this entries 'uses up', default of null uses the threshold
    /// </summary>
    [DataField]
    public int? Rating { get; private set; }

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
    public List<EntityEffectCondition> Conditions { get; private set; } = new();

    /// <summary>
    /// List of effects to perform on the entity if this withdrawal entry is chosen
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = default!;

}

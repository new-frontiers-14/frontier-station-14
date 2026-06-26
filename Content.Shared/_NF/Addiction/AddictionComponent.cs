using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NF.Addiction;

[RegisterComponent]
public sealed partial class AddictionComponent : Component
{
    /// <summary>
    /// How fast or slow this entity gets addicted to anything compared to others
    /// </summary>
    [DataField, ViewVariables]
    public float Multiplier = 1f;

    /// <summary>
    /// Mapping of addiction types to their current rating, current withdrawal rating and next checks for addiction and withdrawal
    /// </summary>
    [DataField, ViewVariables]
    public Dictionary<ProtoId<AddictionPrototype>, AddictionData> Addictions { get; private set; } = new();
}

[DataDefinition]
public sealed partial class AddictionData
{
    /// <summary>
    /// How fast or slow this entity gets addicted to this specific addiction type
    /// </summary>
    [DataField, ViewVariables]
    public float Multiplier = 1f;

    /// <summary>
    /// Current 'high' rating, this is increased everytime a AddictionEffect is applied, and decreases over time
    /// When it hits certain thresholds, withdrawal rating increases
    /// </summary>
    [DataField, ViewVariables]
    public int High;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCheck;

    /// <summary>
    /// Addiction rating. If the 'high' rating is below this then withdrawal effects will be applied
    /// </summary>
    [DataField, ViewVariables]
    public int Addiction;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextWithdrawal;
}

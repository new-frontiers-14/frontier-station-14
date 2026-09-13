using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NF.Addiction;

[RegisterComponent]
[Access(typeof(SharedAddictionSystem))]
public sealed partial class AddictionComponent : Component
{
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
    /// Last reagent to contribute to the addiction so craving messages can be more direct
    /// </summary>
    [ViewVariables]
    public ProtoId<ReagentPrototype>? LastReagent;

    /// <summary>
    /// The last time the entity had their high added to
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables]
    public TimeSpan LastHit;

    /// <summary>
    /// Current 'high' rating, this is increased everytime a AddictionEffect is applied, and decreases over time
    /// When it hits certain thresholds, withdrawal rating increases
    /// </summary>
    [DataField]
    public FixedPoint2 High;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables]
    public TimeSpan NextCheck;

    /// <summary>
    /// Addiction rating. If the 'high' rating is below this then withdrawal effects will be applied
    /// </summary>
    [DataField]
    public FixedPoint2 Addiction;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables]
    public TimeSpan NextWithdrawal;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 Withdrawal => FixedPoint2.Max(0, Addiction - High);
}

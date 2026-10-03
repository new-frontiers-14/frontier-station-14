using Content.Shared._NF.Addiction;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.EntityEffects;

public sealed partial class AdjustAddiction : EventEntityEffect<AdjustAddiction>
{
    /// <summary>
    /// What addiction does this feed or make the entity succumb to (or counter/cure)
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AddictionPrototype> Addiction { get; private set; } = default!;

    /// <summary>
    /// How potent per use is this effect to the 'high', default is 1
    /// </summary>
    [DataField]
    public FixedPoint2 HighAmount { get; private set; } = 1;

    /// <summary>
    /// How potent per use is this effect to the addiction rating, default is 0
    /// This should probably be used more for detox medicines with a negative quantity
    /// </summary>
    [DataField]
    public FixedPoint2 AddictionAmount { get; private set; } = 0;

    [DataField]
    public float? DelayNextCheck = null;

    /// <summary>
    /// If the addiction amount should scale by the quantity of the substance being metabolized, otherwise scales based on percent of max metabolism amount
    /// </summary>
    [DataField]
    public bool ScaleByQuantity { get; private set; }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null; //TODO: provide *something* for the guide book on this
    }
}

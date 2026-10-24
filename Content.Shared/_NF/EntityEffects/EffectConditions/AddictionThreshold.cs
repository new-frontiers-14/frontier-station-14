using Content.Shared._NF.Addiction;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.EntityEffects.Effect;

public sealed partial class AddictionThreshold : EventEntityEffectCondition<AddictionThreshold>
{
    [DataField]
    public FixedPoint2 Min = 0;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 MinAddiction = 0;

    [DataField]
    public FixedPoint2 MaxAddiction = FixedPoint2.MaxValue;

    [DataField]
    public ProtoId<AddictionPrototype>? Addiction = default!;

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "TODO";
    }
}

using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction.EntityEffects;

public sealed partial class AddictionThreshold : EntityEffectCondition
{
    [DataField]
    public FixedPoint2 Min = 0;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 MinAddiction = 0;

    [DataField]
    public FixedPoint2 MaxAddiction = FixedPoint2.MaxValue;

    [DataField(required: true)]
    public ProtoId<AddictionPrototype> Addiction { get; private set; }
    public override bool Condition(EntityEffectBaseArgs args)
    {
        FixedPoint2 high = 0;
        FixedPoint2 addiction = 0;
        if (args.EntityManager.TryGetComponent<AddictionComponent>(args.TargetEntity, out var addictionComp) && addictionComp.Addictions.TryGetValue(Addiction, out var addictData))
        {
            high = addictData.High;
            addiction = addictData.Addiction;
        }
        return high >= Min && high <= Max &&
                addiction >= MinAddiction && addiction <= MaxAddiction;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "TODO";
    }
}

using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction.EntityEffects;

public sealed partial class AddictionThreshold : EntityEffectCondition
{
    [DataField]
    public int Min = 0;

    [DataField]
    public int Max = int.MaxValue;

    [DataField]
    public int MinAddiction = 0;

    [DataField]
    public int MaxAddiction = int.MaxValue;

    [DataField(required: true)]
    public ProtoId<AddictionPrototype> Addiction { get; private set; }
    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent<AddictionComponent>(args.TargetEntity, out var addictionComp))
        {
            FixedPoint2 high = 0;
            FixedPoint2 addiction = 0;
            if (addictionComp.Addictions.TryGetValue(Addiction, out var addictData))
            {
                high = addictData.High;
                addiction = addictData.Addiction;
            }
            return high >= Min && high <= Max &&
                addiction >= MinAddiction && addiction <= MaxAddiction;
        }
        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "TODO";
    }
}

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
            FixedPoint2 rating = 0;
            FixedPoint2 withdrawal = 0;
            if (addictionComp.Addictions.TryGetValue(Addiction, out var addictData))
            {
                rating = addictData.High;
                withdrawal = addictData.Addiction;
            }
            return rating >= Min && rating <= Max &&
                withdrawal >= MinAddiction && withdrawal <= MaxAddiction;
        }
        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "TODO";
    }
}

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction.EntityEffects;

public sealed partial class AddictionThreshold : EntityEffectCondition
{
    [DataField]
    public int Min = 0;

    [DataField]
    public int Max = int.MaxValue;

    [DataField]
    public int MinWithdrawal = 0;

    [DataField]
    public int MaxWithdrawal = int.MaxValue;

    [DataField(required: true)]
    public ProtoId<AddictionPrototype> Addiction { get; private set; }
    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent<AddictionComponent>(args.TargetEntity, out var addictionComp))
        {
            var rating = 0;
            var withdrawal = 0;
            if (addictionComp.Addictions.TryGetValue(Addiction, out var addictData))
            {
                rating = addictData.Rating;
                withdrawal = addictData.Withdrawal;
            }
            return rating >= Min && rating <= Max &&
                withdrawal >= MinWithdrawal && withdrawal <= MaxWithdrawal;
        }
        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "TODO";
    }
}

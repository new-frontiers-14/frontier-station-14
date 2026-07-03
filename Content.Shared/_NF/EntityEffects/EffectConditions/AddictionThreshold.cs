using Content.Shared._NF.Addiction;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Exceptions;

namespace Content.Shared._NF.EntityEffects.Effect;

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

    [DataField]
    public ProtoId<AddictionPrototype>? Addiction = default!;
    public override bool Condition(EntityEffectBaseArgs args)
    {
        var addictionId = Addiction;
        if (args is EntityEffectWithdrawalArgs withdrawalArgs) //Default to the symptom's addiction if no addiction was specified
        {
            addictionId = Addiction ?? withdrawalArgs.Addiction;
        }
        else if (addictionId is null)
        {
            throw new NotImplementedException($"{nameof(Addiction)} field must be set when using {GetType().Name} outside of symptoms");
        }
        var system = args.EntityManager.System<SharedAddictionSystem>();
        var high = system.GetHigh(args.TargetEntity, addictionId.Value);
        var addiction = system.GetAddiction(args.TargetEntity, addictionId.Value);

        return high >= Min && high <= Max &&
                addiction >= MinAddiction && addiction <= MaxAddiction;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "TODO";
    }
}

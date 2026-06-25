using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Addiction.EntityEffects;

public sealed partial class AdjustAddiction : EntityEffect
{
    /// <summary>
    /// What addiction does this feed or make the entity succumb to (or counter/cure)
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AddictionPrototype> Addiction { get; private set; } = default!;

    /// <summary>
    /// How potent per use is this effect to the addiction rating, default is 1
    /// </summary>
    [DataField]
    public int Amount { get; private set; } = 1;

    /// <summary>
    /// How potent per use is this effect to the withdrawal rating, default is 0
    /// </summary>
    [DataField]
    public int Withdrawal { get; private set; } = 0;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TrySystem<SharedAddictionSystem>(out var addictSystem))
        {
            addictSystem.AddAddictionRating(args.TargetEntity, Addiction, Amount);
            addictSystem.AddWithdrawalRating(args.TargetEntity, Addiction, Withdrawal);
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}

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
    /// How potent per use is this effect to the 'high', default is 1
    /// </summary>
    [DataField]
    public int HighAmount { get; private set; } = 1;

    /// <summary>
    /// How potent per use is this effect to the addiction rating, default is 0
    /// </summary>
    [DataField]
    public int AddictionAmount { get; private set; } = 0;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TrySystem<SharedAddictionSystem>(out var addictSystem))
        {
            addictSystem.AddAddictionHighRating(args.TargetEntity, Addiction, HighAmount);
            addictSystem.AddAddictionRating(args.TargetEntity, Addiction, AddictionAmount);
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}

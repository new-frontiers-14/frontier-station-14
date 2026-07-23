using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction;

[ByRefEvent]
public readonly record struct AddAddictionHighRatingEvent(ProtoId<AddictionPrototype> ProtoId, FixedPoint2 Amount, ReagentPrototype? Reagent);
[ByRefEvent]
public readonly record struct AddAddictionRatingEvent(ProtoId<AddictionPrototype> ProtoId, FixedPoint2 Amount, ReagentPrototype? Reagent);

public record class EntityEffectWithdrawalArgs : EntityEffectBaseArgs
{
    public readonly AddictionPrototype Addiction;
    public readonly FixedPoint2 Withdrawal;
    public readonly ReagentPrototype LastReagent;
    public readonly TimeSpan TimeSinceHit;
    public EntityEffectWithdrawalArgs(EntityUid targetEntity, IEntityManager entityManager, AddictionPrototype addiction, FixedPoint2 withdrawal, ReagentPrototype lastReagent, TimeSpan timeSinceHit) : base(targetEntity, entityManager)
    {
        Addiction = addiction;
        Withdrawal = withdrawal;
        LastReagent = lastReagent;
        TimeSinceHit = timeSinceHit;
    }
}

[ByRefEvent]
public record struct GetAddictionModifierEvent(ProtoId<AddictionPrototype> ProtoId, float AddModifier, float SubModifier);

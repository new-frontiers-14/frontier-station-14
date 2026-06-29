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
    public AddictionPrototype Addiction;
    public FixedPoint2 Withdrawal;
    public ReagentPrototype LastReagent;
    public EntityEffectWithdrawalArgs(EntityUid targetEntity, IEntityManager entityManager, AddictionPrototype addiction, FixedPoint2 withdrawal, ReagentPrototype lastReagent) : base(targetEntity, entityManager)
    {
        Addiction = addiction;
        Withdrawal = withdrawal;
        LastReagent = lastReagent;
    }
}

[ByRefEvent]
public record struct GetAddictionModifierEvent
{
    public ProtoId<AddictionPrototype> ProtoId { get; init; }
    public float Modifier = 1f;

    public GetAddictionModifierEvent(ProtoId<AddictionPrototype> protoId, float modifier)
    {
        ProtoId = protoId;
        Modifier = modifier;
    }
}

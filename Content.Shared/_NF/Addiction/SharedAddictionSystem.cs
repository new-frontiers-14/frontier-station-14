using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction;

public abstract partial class SharedAddictionSystem : EntitySystem
{
    public void AddAddictionHighRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        EnsureComp<AddictionComponent>(uid); //temporary until we update player species to have the addiction component
        var ev = new AddAddictionHighRatingEvent(protoId, amount, reagent);
        RaiseLocalEvent(uid, ref ev);
    }
    public void AddAddictionRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        EnsureComp<AddictionComponent>(uid);
        var ev = new AddAddictionRatingEvent(protoId, amount, reagent);
        RaiseLocalEvent(uid, ref ev);
    }
}

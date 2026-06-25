using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction;

public abstract partial class SharedAddictionSystem : EntitySystem
{
    public void AddAddictionRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, int amount)
    {
        EnsureComp<AddictionComponent>(uid); //temporary until we update player species to have the addiction component
        var ev = new AddAddictionRatingEvent(protoId, amount);
        RaiseLocalEvent(uid, ref ev);
    }
    public void AddWithdrawalRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, int amount)
    {
        EnsureComp<AddictionComponent>(uid);
        var ev = new AddWithdrawalRatingEvent(protoId, amount);
        RaiseLocalEvent(uid, ref ev);
    }
}

using Content.Shared._NF.Addiction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Addiction;

public sealed partial class AddictionSystem : SharedAddictionSystem
{
    public override void AddAddictionHighRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        //handled serverside
    }

    public override void AddAddictionRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        //handled serverside
    }
}

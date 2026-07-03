using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction;

public abstract partial class SharedAddictionSystem : EntitySystem
{
    public void AddAddictionHighRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        if (HasComp<GodmodeComponent>(uid))
            return;
        EnsureComp<AddictionComponent>(uid);
        var ev = new AddAddictionHighRatingEvent(protoId, amount, reagent);
        RaiseLocalEvent(uid, ref ev);
    }
    public void AddAddictionRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        if (HasComp<GodmodeComponent>(uid))
            return;
        EnsureComp<AddictionComponent>(uid);
        var ev = new AddAddictionRatingEvent(protoId, amount, reagent);
        RaiseLocalEvent(uid, ref ev);
    }

    #region Getters
    public FixedPoint2 GetHigh(Entity<AddictionComponent?> entity, ProtoId<AddictionPrototype> addiction)
    {
        if (!Resolve(entity, ref entity.Comp))
        {
            return 0;
        }

        return GetHigh(entity.Comp, addiction);
    }

    public FixedPoint2 GetHigh(AddictionComponent component, ProtoId<AddictionPrototype> addiction)
    {
        if (!component.Addictions.TryGetValue(addiction, out var addictData))
        {
            return 0;
        }

        return addictData.High;
    }

    public FixedPoint2 GetAddiction(Entity<AddictionComponent?> entity, ProtoId<AddictionPrototype> addiction)
    {
        if (!Resolve(entity, ref entity.Comp))
        {
            return 0;
        }

        return GetAddiction(entity.Comp, addiction);
    }

    public FixedPoint2 GetAddiction(AddictionComponent component, ProtoId<AddictionPrototype> addiction)
    {
        if (!component.Addictions.TryGetValue(addiction, out var addictData))
        {
            return 0;
        }

        return addictData.Addiction;
    }

    public FixedPoint2 GetWithdrawal(Entity<AddictionComponent?> entity, ProtoId<AddictionPrototype> addiction)
    {
        if (!Resolve(entity, ref entity.Comp))
        {
            return 0;
        }

        return GetWithdrawal(entity.Comp, addiction);
    }

    public FixedPoint2 GetWithdrawal(AddictionComponent component, ProtoId<AddictionPrototype> addiction)
    {
        if (!component.Addictions.TryGetValue(addiction, out var addictData))
        {
            return 0;
        }

        return addictData.Withdrawal;
    }

    #endregion
}

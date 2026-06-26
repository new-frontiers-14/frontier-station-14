using System.Linq;
using Content.Shared._NF.Addiction;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._NF.Addiction;

public sealed partial class AddictionSystem : SharedAddictionSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, AddAddictionHighRatingEvent>(OnAddHighRating);
        SubscribeLocalEvent<AddictionComponent, AddAddictionRatingEvent>(OnAddAddictionRating);
        SubscribeLocalEvent<AddictionComponent, RejuvenateEvent>(OnRejuvenate);
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


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var addicts = new ValueList<(EntityUid, AddictionComponent)>(Count<AddictionComponent>());
        var query = EntityQueryEnumerator<AddictionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            addicts.Add((uid, comp));
        }

        foreach (var (uid, addict) in addicts)
        {
            foreach (var (addictProtoId, addictData) in addict.Addictions)
            {
                ApplyHighUpdate(uid, addictProtoId, addictData);
                ApplyWithdrawalUpdate(uid, addictProtoId, addictData);
            }
        }
    }

    private void ApplyHighUpdate(EntityUid uid, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
    {
        if (_gameTiming.CurTime < data.NextCheck)
        {
            return;
        }
        if (data.High == 0 && data.Addiction == 0)
        {
            data.NextWithdrawal = data.NextCheck = TimeSpan.MaxValue; //should effectively disable this entry
            return;
        }

        if (!_prototypeManager.TryIndex(addictProtoId, out var addictProto))
        {
            Log.Error("Unable to find Addiction with Prototype ID: {0}", addictProtoId.Id);
            data.NextCheck = TimeSpan.MaxValue; //so we don't spam the logs too badly
            return;
        }

        data.NextCheck = _gameTiming.CurTime + addictProto.CheckPeriod;

        //Reduce addiction, then the High, since withdrawal depends on High
        var withdrawal = data.Withdrawal;
        if (withdrawal > 0)
        {
            var deltaAddiction = withdrawal * addictProto.Withdrawal.DecayRate;
            var evAddiction = new AddAddictionRatingEvent(addictProtoId, -deltaAddiction);
            RaiseLocalEvent(uid, ref evAddiction);
        }

        if (data.High > 0)
        {
            var ev = new AddAddictionHighRatingEvent(addictProtoId, -addictProto.DecayRate * data.High);
            RaiseLocalEvent(uid, ref ev);
        }

    }

    private void ApplyWithdrawalUpdate(EntityUid uid, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
    {
        if (_gameTiming.CurTime < data.NextWithdrawal)
        {
            return;
        }

        if (data.Addiction == 0) //disable future withdrawal checks for now
        {
            data.NextWithdrawal = TimeSpan.MaxValue;
            return;
        }

        var withdrawal = data.Withdrawal;
        if (withdrawal <= 0) //Addiction is sated for now
        {
            data.NextWithdrawal = data.NextCheck;//When the addiction rating changes next should we see about applying withdrawals
            return;
        }

        if (!_prototypeManager.TryIndex(addictProtoId, out var addictProto))
        {
            Log.Error("Unable to find Addiction with Prototype ID: {0}", addictProtoId.Id);
            data.NextWithdrawal = TimeSpan.MaxValue; //so we don't spam the logs
            return;
        }


        var effectArgs = new EntityEffectBaseArgs(uid, EntityManager);
        var eligibleSymptoms = addictProto.Withdrawal.Symptoms.Where(e => e.ShouldApply(withdrawal, effectArgs, _random)).ToList();
        if (eligibleSymptoms.Count == 0)
        {
            data.NextWithdrawal = data.NextCheck;
            return;
        }

        _random.Shuffle(eligibleSymptoms); //don't apply effects in a completely predictable fashion each time

        var nextWithdrawal = _gameTiming.CurTime;
        var i = 0;
        while (withdrawal > 0 && eligibleSymptoms.Count > 0)
        {
            var symptom = eligibleSymptoms[i % eligibleSymptoms.Count];
            withdrawal -= symptom.Rating ?? symptom.Min;
            foreach (var effect in symptom.Effects)
            {
                if (!effect.ShouldApply(effectArgs, _random)) //Each effect could have their own conditions to be applied separate from the entire symptom
                {
                    continue;
                }
                effect.Effect(effectArgs);
            }
            if (!symptom.Repeatable)
            {
                eligibleSymptoms.Remove(symptom);
            }
            nextWithdrawal += symptom.Duration;
            i++;
        }
        data.NextWithdrawal = nextWithdrawal;


    }

    private void OnRejuvenate(Entity<AddictionComponent> ent, ref RejuvenateEvent args)
    {
        // TODO: May need to look into cancelling any active withdrawal effects
        // iterating through existing addictions to leave room for setting up multipliers for specific addiction types
        foreach (var (_, addictData) in ent.Comp.Addictions)
        {
            addictData.Addiction = 0;
            addictData.High = 0;
            addictData.NextCheck = TimeSpan.MaxValue; //Disables checking
            addictData.NextWithdrawal = TimeSpan.MaxValue;
        }

    }

    private void OnAddHighRating(Entity<AddictionComponent> entity, ref AddAddictionHighRatingEvent args)
    {
        if (!_prototypeManager.TryIndex(args.ProtoId, out var addictProto))
        {
            return;
        }

        UpdateHighInternal(entity, addictProto, args.Amount);

    }

    private void UpdateHighInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, FixedPoint2 amount)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        addictData.High += entity.Comp.Multiplier * addictData.Multiplier * amount;
        addictData.High = FixedPoint2.Max(0, addictData.High);  //prevent going below 0

        if (addictData.High > addictProto.Threshold)
        {
            var level = addictData.High * addictProto.Withdrawal.Multiplier; // - addictProto.Threshold;
            addictData.Addiction = FixedPoint2.Max(addictData.Addiction, level);
            addictData.NextWithdrawal = _gameTiming.CurTime + addictProto.CheckPeriod;
        }

        //Only reset check timing if we added to the addiction rating, not if we removed it by some effect
        if (amount >= 0)
        {
            addictData.NextCheck = _gameTiming.CurTime + addictProto.CheckPeriod;
        }

    }

    private void OnAddAddictionRating(Entity<AddictionComponent> entity, ref AddAddictionRatingEvent args)
    {
        if (!_prototypeManager.TryIndex(args.ProtoId, out var addictProto))
        {
            return;
        }

        UpdateAddictionInternal(entity, addictProto, args.Amount);
    }

    private void UpdateAddictionInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, FixedPoint2 amount)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        addictData.Addiction = FixedPoint2.Max(0, addictData.Addiction + amount);
        if (addictProto.Withdrawal.Max is not null)
        {
            addictData.Addiction = FixedPoint2.Min(addictProto.Withdrawal.Max.Value, addictData.Addiction);
        }
        if (amount >= 0)
        {
            addictData.NextWithdrawal = _gameTiming.CurTime + addictProto.CheckPeriod;
        }
    }
}

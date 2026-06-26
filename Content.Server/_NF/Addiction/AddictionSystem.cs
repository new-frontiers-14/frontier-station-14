using System.Linq;
using Content.Shared._NF.Addiction;
using Content.Shared.EntityEffects;
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
                ApplyHighUpdate(uid, addict, addictProtoId, addictData);
                ApplyAddictionUpdate(uid, addict, addictProtoId, addictData);
            }
        }
    }

    private void ApplyHighUpdate(EntityUid uid, AddictionComponent addictComp, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
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

        if (data.High > 0)
        {
            var desiredHigh = (int)Math.Floor(data.High * addictProto.DecayRate);
            var ev = new AddAddictionHighRatingEvent(addictProtoId, desiredHigh - data.High);
            RaiseLocalEvent(uid, ref ev);
            return;
        }

        //Rating is 0 but withdrawal is not
        var desiredAddiction = (int)Math.Floor(data.Addiction * addictProto.Withdrawal.DecayRate);
        var evAddiction = new AddAddictionRatingEvent(addictProtoId, desiredAddiction - data.Addiction);
        RaiseLocalEvent(uid, ref evAddiction);

    }

    private void ApplyAddictionUpdate(EntityUid uid, AddictionComponent addictComp, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
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

        var withdrawal = data.Addiction - data.High;
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

        var eligibleSymptoms = addictProto.Withdrawal.Symptoms.Where(e => e.Min <= withdrawal).ToList();
        if (eligibleSymptoms.Count == 0)
        {
            data.NextWithdrawal = data.NextCheck;
            return;
        }

        _random.Shuffle(eligibleSymptoms); //don't apply effects in a completely predictable fashion each time

        var nextWithdrawal = _gameTiming.CurTime;
        var i = 0;
        var effectArgs = new EntityEffectBaseArgs(uid, EntityManager);
        while (withdrawal > 0 && eligibleSymptoms.Count > 0)
        {
            var symptom = eligibleSymptoms[i % eligibleSymptoms.Count];
            withdrawal -= symptom.Rating ?? symptom.Min;
            foreach (var effect in symptom.Effects)
            {
                if (!effect.ShouldApply(effectArgs, _random))
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


    public void OnAddHighRating(Entity<AddictionComponent> entity, ref AddAddictionHighRatingEvent args)
    {
        if (!_prototypeManager.TryIndex(args.ProtoId, out var addictProto))
        {
            return;
        }

        UpdateHighInternal(entity, addictProto, args.Amount);

    }

    private void UpdateHighInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, int amount)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        addictData.High += (int)(entity.Comp.Multiplier * addictData.Multiplier * amount);
        addictData.High = Math.Max(0, addictData.High); //prevent going below 0

        if (addictData.High > addictProto.Threshold)
        {
            var level = (int)(addictData.High * addictProto.Withdrawal.Multiplier); // - addictProto.Threshold;
            addictData.Addiction = Math.Max(addictData.Addiction, level);
            addictData.NextWithdrawal = _gameTiming.CurTime + addictProto.CheckPeriod;
        }

        //Only reset check timing if we added to the addiction rating, not if we removed it by some effect
        if (amount >= 0)
        {
            addictData.NextCheck = _gameTiming.CurTime + addictProto.CheckPeriod;
        }

    }

    public void OnAddAddictionRating(Entity<AddictionComponent> entity, ref AddAddictionRatingEvent args)
    {
        if (!_prototypeManager.TryIndex(args.ProtoId, out var addictProto))
        {
            return;
        }

        UpdateAddictionInternal(entity, addictProto, args.Amount);
    }

    private void UpdateAddictionInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, int amount)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        addictData.Addiction = Math.Max(0, addictData.Addiction + amount);
        if (addictProto.Withdrawal.Max is not null)
        {
            addictData.Addiction = Math.Min(addictProto.Withdrawal.Max.Value, addictData.Addiction);
        }
        if (amount >= 0)
        {
            addictData.NextWithdrawal = _gameTiming.CurTime + addictProto.CheckPeriod;
        }
    }
}

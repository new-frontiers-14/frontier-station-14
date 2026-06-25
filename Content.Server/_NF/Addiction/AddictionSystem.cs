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

        SubscribeLocalEvent<AddictionComponent, AddAddictionRatingEvent>(OnAddAddictionRating);
        SubscribeLocalEvent<AddictionComponent, AddWithdrawalRatingEvent>(OnAddWithdrawalRating);

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
                ApplyAddictUpdate(uid, addict, addictProtoId, addictData);
                ApplyWithdrawalUpdate(uid, addict, addictProtoId, addictData);
            }
        }
    }

    private void ApplyAddictUpdate(EntityUid uid, AddictionComponent addictComp, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
    {
        if (_gameTiming.CurTime < data.NextCheck)
        {
            return;
        }
        if (data.Rating == 0 && data.Withdrawal == 0)
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

        if (data.Rating > 0)
        {
            var desiredAmount = (int)Math.Floor(data.Rating * addictProto.DecayRate);
            var ev = new AddAddictionRatingEvent(addictProtoId, desiredAmount - data.Rating);
            RaiseLocalEvent(uid, ref ev);
            return;
        }

        //Rating is 0 but withdrawal is not
        var desiredWithdrawal = (int)Math.Floor(data.Withdrawal * addictProto.Withdrawal.DecayRate);
        var evWithdrawal = new AddWithdrawalRatingEvent(addictProtoId, desiredWithdrawal - data.Withdrawal);
        RaiseLocalEvent(uid, ref evWithdrawal);

    }

    private void ApplyWithdrawalUpdate(EntityUid uid, AddictionComponent addictComp, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
    {
        if (_gameTiming.CurTime < data.NextWithdrawal)
        {
            return;
        }

        if (data.Withdrawal == 0) //disable future withdrawal checks for now
        {
            data.NextWithdrawal = TimeSpan.MaxValue;
            return;
        }

        var intensity = data.Withdrawal - data.Rating;
        if (intensity <= 0) //Addiction is sated for now
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

        var eligibleEntries = addictProto.Withdrawal.Entries.Where(e => e.Threshold <= intensity).ToList();
        if (eligibleEntries.Count == 0)
        {
            data.NextWithdrawal = data.NextCheck;
            return;
        }

        _random.Shuffle(eligibleEntries); //don't apply effects in a completely predictable fashion each time

        var nextWithdrawal = _gameTiming.CurTime;
        var i = 0;
        var effectArgs = new EntityEffectBaseArgs(uid, EntityManager);
        while (intensity > 0 && eligibleEntries.Count > 0)
        {
            var entry = eligibleEntries[i % eligibleEntries.Count];
            intensity -= entry.Rating ?? entry.Threshold;
            foreach (var effect in entry.Effects)
            {
                if (!effect.ShouldApply(effectArgs, _random))
                {
                    continue;
                }
                effect.Effect(effectArgs);
            }
            if (!entry.Repeatable)
            {
                eligibleEntries.Remove(entry);
            }
            nextWithdrawal += entry.Duration;
            i++;
        }
        data.NextWithdrawal = nextWithdrawal;


    }


    public void OnAddAddictionRating(Entity<AddictionComponent> entity, ref AddAddictionRatingEvent args)
    {
        if (!_prototypeManager.TryIndex(args.ProtoId, out var addictProto))
        {
            return;
        }

        UpdateAddictionRatingInternal(entity, addictProto, args.Amount);

    }

    private void UpdateAddictionRatingInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, int amount)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        addictData.Rating += (int)(entity.Comp.Multiplier * addictData.Multiplier * amount);
        addictData.Rating = Math.Max(0, addictData.Rating); //prevent going below 0

        if (addictData.Rating > addictProto.Threshold)
        {
            var level = addictData.Rating - addictProto.Threshold;
            addictData.Withdrawal = Math.Max(addictData.Withdrawal, level);
            addictData.NextWithdrawal = _gameTiming.CurTime + addictProto.CheckPeriod;
        }

        //Only reset check timing if we added to the addiction rating, not if we removed it by some effect
        if (amount >= 0)
        {
            addictData.NextCheck = _gameTiming.CurTime + addictProto.CheckPeriod;
        }

    }

    public void OnAddWithdrawalRating(Entity<AddictionComponent> entity, ref AddWithdrawalRatingEvent args)
    {
        if (!_prototypeManager.TryIndex(args.ProtoId, out var addictProto))
        {
            return;
        }

        UpdateWithdrawalRatingInternal(entity, addictProto, args.Amount);
    }

    private void UpdateWithdrawalRatingInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, int amount)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        addictData.Withdrawal = Math.Max(0, addictData.Withdrawal + amount);
        if (addictProto.Withdrawal.Max is not null)
        {
            addictData.Withdrawal = Math.Min(addictProto.Withdrawal.Max.Value, addictData.Withdrawal);
        }
        if (amount >= 0)
        {
            addictData.NextWithdrawal = _gameTiming.CurTime + addictProto.CheckPeriod;
        }
    }
}

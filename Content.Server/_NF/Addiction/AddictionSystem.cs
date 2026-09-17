using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._NF.Addiction;
using Content.Shared._NF.EntityEffects;
using Content.Shared._NF.EntityEffects.Effect;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
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
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popupSys = default!;

    /// <summary>
    /// Minimum rating for High and Addiction to be considered active. Because this system does exponential decay often when at 0.1, it never goes to 0 keeping the component active indefinitely
    /// </summary>
    public static readonly FixedPoint2 MinRating = 0.1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<AddictionModifierComponent, GetAddictionModifierEvent>(OnGetAddictionModifier);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<AdjustAddiction>>(OnAdjustAddiction);
        SubscribeLocalEvent<CheckEntityEffectConditionEvent<AddictionThreshold>>(OnCheckAddictionThreshold);
    }


    #region Updates/Withdrawals
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var count = Count<AddictionComponent>();
        if (count == 0)
        {
            return;
        }
        var addicts = new ValueList<(EntityUid, AddictionComponent)>(count);
        var query = EntityQueryEnumerator<AddictionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_mobState.IsIncapacitated(uid)) //for now ignore crit or dead mobs with addictions
            {
                addicts.Add((uid, comp));
            }
        }
        if (addicts.Count == 0)
        {
            //Nothing to do
            return;
        }

        var addictionsToRemove = new ValueList<ProtoId<AddictionPrototype>>();
        var componentsToRemove = new ValueList<EntityUid>();
        foreach (var (uid, addict) in addicts)
        {
            if (addict.Addictions.Count == 0)
            {
                componentsToRemove.Add(uid);
                continue;
            }
            addictionsToRemove.Clear();
            foreach (var (addictProtoId, addictData) in addict.Addictions)
            {
                if (addictData.High <= MinRating && addictData.Addiction <= MinRating)
                {
                    addictionsToRemove.Add(addictProtoId);
                    continue;
                }

                ApplyHighUpdate((uid, addict), addictProtoId, addictData);
                ApplyWithdrawalUpdate((uid, addict), addictProtoId, addictData);
            }

            foreach (var addictToRemove in addictionsToRemove)
            {
                addict.Addictions.Remove(addictToRemove);
            }
        }

        //Clean up component if entity is not addicted to anything
        foreach (var toRemove in componentsToRemove)
        {
            RemComp<AddictionComponent>(toRemove);
        }

    }

    private void ApplyHighUpdate(Entity<AddictionComponent> entity, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
    {
        if (_gameTiming.CurTime < data.NextCheck)
        {
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
            UpdateAddictionInternal(entity, addictProto, -addictProto.Withdrawal.DecayRate * withdrawal, null);
        }

        if (data.High > 0)
        {
            UpdateHighInternal(entity, addictProto, -addictProto.DecayRate * data.High, null);
        }

    }

    private void ApplyWithdrawalUpdate(Entity<AddictionComponent> entity, ProtoId<AddictionPrototype> addictProtoId, AddictionData data)
    {
        if (_gameTiming.CurTime < data.NextWithdrawal)
        {
            return;
        }

        if (data.Addiction <= MinRating) //disable future withdrawal checks for now
        {
            data.NextWithdrawal = TimeSpan.MaxValue;
            return;
        }

        if (!_prototypeManager.TryIndex(addictProtoId, out var addictProto))
        {
            Log.Error("Unable to find Addiction with Prototype ID: {0}", addictProtoId.Id);
            data.NextWithdrawal = TimeSpan.MaxValue; //so we don't spam the logs
            return;
        }

        var withdrawal = data.Withdrawal;
        if (withdrawal <= 0) //Addiction is sated for now
        {
            data.NextWithdrawal = data.NextCheck; //no need to check for withdrawals until the addiction/high is updated
            return;
        }

        if (!_prototypeManager.TryIndex(data.LastReagent, out var lastReagent))
        {
            lastReagent = _prototypeManager.Index(addictProto.DefaultReagent);
        }

        var effectArgs = new EntityEffectWithdrawalArgs(entity, EntityManager, addictProto, withdrawal, lastReagent, _gameTiming.CurTime - data.LastHit);
        var eligibleSymptoms = addictProto.Withdrawal.Symptoms.Where(e => e.ShouldApply(effectArgs, _random)).ToList();
        if (eligibleSymptoms.Count == 0)
        {
            data.NextWithdrawal = _gameTiming.CurTime + addictProto.Withdrawal.MinCheckDelay;
            return;
        }

        _random.Shuffle(eligibleSymptoms); //don't apply effects in a completely predictable fashion each time

        SymptomMessageList? messageList = null;
        int messagePriority = int.MinValue;
        var nextWithdrawal = _gameTiming.CurTime;
        var i = 0;
        while (withdrawal > 0 && eligibleSymptoms.Count > 0)
        {
            var symptom = eligibleSymptoms[i % eligibleSymptoms.Count];

            if (ShouldUseSymptomMessage(symptom.Messages, messagePriority))
            {
                messageList = symptom.Messages;
                messagePriority = messageList.Priority;
            }

            withdrawal -= symptom.Rating;
            if (symptom.Effects is not null)
            {
                foreach (var effect in symptom.Effects)
                {
                    if (!effect.ShouldApply(effectArgs, _random)) //Each effect could have their own conditions to be applied separate from the entire symptom
                    {
                        continue;
                    }
                    effect.Effect(effectArgs);
                }
            }
            if (!symptom.Repeatable)
            {
                eligibleSymptoms.Remove(symptom);
            }
            nextWithdrawal += symptom.Duration;
            i++;
        }

        ApplySymptomPopup(messageList, effectArgs);

        data.NextWithdrawal = nextWithdrawal;


    }

    private bool ShouldUseSymptomMessage([NotNullWhen(true)] SymptomMessageList? message, int priority)
    {
        if (message is null)
            return false;
        if (message.Priority <= priority)
            return false;
        if (message.Probability < 1.0f && !_random.Prob(message.Probability))
            return false;

        return true;
    }

    private void ApplySymptomPopup(SymptomMessageList? messageList, EntityEffectWithdrawalArgs args)
    {
        if (messageList is null)
            return;

        LocId msg;
        if (_prototypeManager.Resolve(messageList.DataSet, out var dataSet))
        {
            msg = _random.Pick(dataSet.Values);
        }
        else if (messageList.List is not null && messageList.List.Length > 0)
        {
            msg = _random.Pick(messageList.List);
        }
        else
        {
            Log.Error("Symptom Message List should define a DataSet or List attribute");
            return;
        }

        var formatted = Loc.GetString(msg, [
                    ("entity", args.TargetEntity),
                    ("addiction", args.Addiction.LocalizedName),
                    ("lastReagent", args.LastReagent.LocalizedName)
                ]);
        if (messageList.Type == PopupRecipients.Local)
        {
            _popupSys.PopupEntity(formatted, args.TargetEntity, args.TargetEntity, messageList.VisualType);
        }
        else
        {
            _popupSys.PopupEntity(formatted, args.TargetEntity, messageList.VisualType);
        }
    }
    #endregion

    public override void AddAddictionHighRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        if (!_prototypeManager.TryIndex(protoId, out var addiction))
            return;
        if (HasComp<GodmodeComponent>(uid))
            return;
        var comp = EnsureComp<AddictionComponent>(uid);
        UpdateHighInternal((uid, comp), addiction, amount, reagent);
    }

    public override void AddAddictionRating(EntityUid uid, ProtoId<AddictionPrototype> protoId, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        if (!_prototypeManager.TryIndex(protoId, out var addiction))
            return;
        if (HasComp<GodmodeComponent>(uid))
            return;
        var comp = EnsureComp<AddictionComponent>(uid);
        UpdateAddictionInternal((uid, comp), addiction, amount, reagent);
    }

    private void OnRejuvenate(Entity<AddictionComponent> ent, ref RejuvenateEvent args)
    {
        RemComp<AddictionComponent>(ent); //Just remove the addiction component to cure someone's addiction
    }
    private void OnAdjustAddiction(ref ExecuteEntityEffectEvent<AdjustAddiction> ev)
    {
        var uid = ev.Args.TargetEntity;
        if (HasComp<GodmodeComponent>(uid))
            return;
        if (!_prototypeManager.TryIndex(ev.Effect.Addiction, out var addictProto))
            return;

        var comp = EnsureComp<AddictionComponent>(uid);
        FixedPoint2 factor = 1f;
        ReagentPrototype? reagent = null;
        if (ev.Args is EntityEffectReagentArgs reagentArgs)
        {
            factor = ev.Effect.ScaleByQuantity ? reagentArgs.Quantity : reagentArgs.Scale;
            reagent = reagentArgs.Reagent;
        }


        UpdateHighInternal((uid, comp), addictProto, ev.Effect.HighAmount * factor, reagent);
        UpdateAddictionInternal((uid, comp), addictProto, ev.Effect.AddictionAmount * factor, reagent);

        if (ev.Effect.DelayNextCheck != null)
        {
            UpdateAddictionNextCheck((uid, comp), addictProto, TimeSpan.FromSeconds(ev.Effect.DelayNextCheck.Value * factor.Float()));
        }
    }

    private void OnCheckAddictionThreshold(ref CheckEntityEffectConditionEvent<AddictionThreshold> ev)
    {
        var args = ev.Args;
        var addictionId = ev.Condition.Addiction;
        if (args is EntityEffectWithdrawalArgs withdrawalArgs) //Default to the symptom's addiction if no addiction was specified
        {
            addictionId ??= withdrawalArgs.Addiction;
        }
        else if (addictionId is null) //Condition can be used outside of a symptom but must specify the addiction
        {
            throw new NotImplementedException($"{nameof(ev.Condition.Addiction)} field must be set when using {typeof(AddictionThreshold).Name} outside of symptoms");
        }
        var high = GetHigh(args.TargetEntity, addictionId.Value);
        var addiction = GetAddiction(args.TargetEntity, addictionId.Value);

        ev.Result = high >= ev.Condition.Min && high <= ev.Condition.Max
            && addiction >= ev.Condition.MinAddiction && addiction <= ev.Condition.MaxAddiction;
    }

    private void OnGetAddictionModifier(Entity<AddictionModifierComponent> ent, ref GetAddictionModifierEvent args)
    {
        //ignore negative values as that will be a very wonky effect
        args.AddModifier *= Math.Max(ent.Comp.AddMultiplier, 0);
        args.SubModifier *= Math.Max(ent.Comp.SubMultiplier, 0);
        if (ent.Comp.Modifiers.TryGetValue(args.ProtoId, out var val))
        {
            args.AddModifier *= Math.Max(val.AddMultiplier, 0);
            args.SubModifier *= Math.Max(val.SubMultiplier, 0);
        }
    }

    private void UpdateHighInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        var ev = new GetAddictionModifierEvent(addictProto, 1f, 1f);
        RaiseLocalEvent(entity, ref ev);

        //prevent negative modifiers
        var modifier = amount > 0 ? ev.AddModifier : ev.SubModifier;
        modifier = Math.Max(0, modifier);

        addictData.High += modifier * amount;
        addictData.High = FixedPoint2.Max(0, addictData.High);  //prevent going below 0

        var nextCheckTime = _gameTiming.CurTime + addictProto.CheckPeriod;
        if (addictData.High > addictProto.Threshold)
        {
            var level = addictData.High * addictProto.Withdrawal.Multiplier;
            addictData.Addiction = FixedPoint2.Max(addictData.Addiction, level);
            if (addictData.NextWithdrawal > nextCheckTime)
            {
                addictData.NextWithdrawal = nextCheckTime;
            }
            addictData.LastReagent = reagent?.ID ?? addictData.LastReagent;
        }

        //Only reset check timing if we added to the addiction rating, not if we removed it by some effect
        if (amount >= 0)
        {
            addictData.LastHit = _gameTiming.CurTime;
            //leave next check time alone if something has set it farther in the future
            //typically from 'DelayNextCheck' on AdjustAddiction
            if (addictData.NextCheck <= nextCheckTime)
            {
                addictData.NextCheck = _gameTiming.CurTime + addictProto.CheckPeriod;
            }
        }

    }

    private void UpdateAddictionInternal(Entity<AddictionComponent> entity, AddictionPrototype addictProto, FixedPoint2 amount, ReagentPrototype? reagent)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addictProto.ID);

        addictData.Addiction = FixedPoint2.Max(0, addictData.Addiction + amount);
        if (addictProto.Max is not null)
        {
            addictData.Addiction = FixedPoint2.Min(addictProto.Max.Value, addictData.Addiction);
        }
        if (amount >= 0)
        {
            addictData.LastReagent = reagent?.ID ?? addictData.LastReagent;
            if (addictData.Withdrawal > 0)
            {
                addictData.NextWithdrawal = _gameTiming.CurTime + addictProto.Withdrawal.MinCheckDelay;
            }
            else
            {
                addictData.NextWithdrawal = addictData.NextCheck;
            }
        }
    }

    private void UpdateAddictionNextCheck(Entity<AddictionComponent> entity, ProtoId<AddictionPrototype> addiction, TimeSpan duration)
    {
        var addictData = entity.Comp.Addictions.GetOrNew(addiction);

        addictData.NextCheck += duration;
    }


}

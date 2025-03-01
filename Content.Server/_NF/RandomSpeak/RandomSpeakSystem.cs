using Content.Server._NF.RandomSpeak.Components;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.VendingMachines;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._NF.RandomSpeak.EntitySystems;

public sealed class RandomSpeakSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <summary>
    /// The maximum amount of time between checking if advertisements should be displayed
    /// </summary>
    private readonly TimeSpan _maximumNextCheckDuration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The next time the game will check if advertisements should be displayed
    /// </summary>
    private TimeSpan _nextCheckTime = TimeSpan.MinValue;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomSpeakComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<MobStateComponent, AttemptRandomSpeakEvent>(OnMobStateRandomSpeakEvent);
        SubscribeLocalEvent<ActorComponent, AttemptRandomSpeakEvent>(OnActorRandomSpeakEvent);

        _nextCheckTime = TimeSpan.MinValue;
    }

    private void OnMapInit(EntityUid uid, RandomSpeakComponent advert, MapInitEvent args)
    {
        var prewarm = advert.Prewarm;
        RandomizeNextAdvertTime(advert, prewarm);
        _nextCheckTime = MathHelper.Min(advert.NextRandomSpeakTime, _nextCheckTime);
    }

    private void RandomizeNextAdvertTime(RandomSpeakComponent advert, bool prewarm = false)
    {
        var minDuration = prewarm ? 0 : Math.Max(1, advert.MinimumWait);
        var maxDuration = Math.Max(minDuration, advert.MaximumWait);
        var waitDuration = TimeSpan.FromSeconds(_random.Next(minDuration, maxDuration));

        advert.NextRandomSpeakTime = _gameTiming.CurTime + waitDuration;
    }

    public void SayRandomSpeak(EntityUid uid, RandomSpeakComponent? advert = null)
    {
        if (!Resolve(uid, ref advert))
            return;

        var attemptEvent = new AttemptRandomSpeakEvent(uid);
        RaiseLocalEvent(uid, ref attemptEvent);
        if (attemptEvent.Cancelled)
            return;

        if (_prototypeManager.TryIndex(advert.Pack, out var advertisements))
            _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(advertisements.Values)), InGameICChatType.Speak, hideChat: true);
    }

    public override void Update(float frameTime)
    {
        var currentGameTime = _gameTiming.CurTime;
        if (_nextCheckTime > currentGameTime)
            return;

        // _nextCheckTime starts at TimeSpan.MinValue, so this has to SET the value, not just increment it.
        _nextCheckTime = currentGameTime + _maximumNextCheckDuration;

        var query = EntityQueryEnumerator<RandomSpeakComponent>();
        while (query.MoveNext(out var uid, out var advert))
        {
            if (currentGameTime > advert.NextRandomSpeakTime)
            {
                SayRandomSpeak(uid, advert);
                // The timer is always refreshed when it expires, to prevent mass advertising (ex: all the vending machines have no power, and get it back at the same time).
                RandomizeNextAdvertTime(advert);
            }
            _nextCheckTime = MathHelper.Min(advert.NextRandomSpeakTime, _nextCheckTime);
        }
    }

    private static void OnMobStateRandomSpeakEvent(EntityUid uid, MobStateComponent mobState, ref AttemptRandomSpeakEvent args)
    {
        args.Cancelled |= mobState.CurrentState != MobState.Alive;
    }

    private static void OnActorRandomSpeakEvent(EntityUid uid, ActorComponent actor, ref AttemptRandomSpeakEvent args)
    {
        args.Cancelled = true;
    }
}

[ByRefEvent]
public record struct AttemptRandomSpeakEvent(EntityUid? RandomSpeakr)
{
    public bool Cancelled = false;
}

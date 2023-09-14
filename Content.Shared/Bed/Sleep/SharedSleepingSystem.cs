using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Speech;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Bed.Sleep
{
    public abstract class SharedSleepingSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly INetManager _net = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly BlindableSystem _blindableSystem = default!;

        [ValidatePrototypeId<EntityPrototype>] private const string WakeActionId = "ActionWake";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SleepingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SleepingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SleepingComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SleepingComponent, CanSeeAttemptEvent>(OnSeeAttempt);
            SubscribeLocalEvent<SleepingComponent, EntityUnpausedEvent>(OnSleepUnpaused);
        }

        private void OnSleepUnpaused(EntityUid uid, SleepingComponent component, ref EntityUnpausedEvent args)
        {
            component.CoolDownEnd += args.PausedTime;
            Dirty(uid, component);
        }

        private void OnStartup(EntityUid uid, SleepingComponent component, ComponentStartup args)
        {
            var ev = new SleepStateChangedEvent(true);
            RaiseLocalEvent(uid, ev);
            _blindableSystem.UpdateIsBlind(uid);

            if (_net.IsClient)
                return;

            component.WakeAction = Spawn(WakeActionId);
            _actionsSystem.SetCooldown(component.WakeAction, _gameTiming.CurTime, _gameTiming.CurTime + TimeSpan.FromSeconds(15));
            _actionsSystem.AddAction(uid, component.WakeAction.Value, null);
        }

        private void OnShutdown(EntityUid uid, SleepingComponent component, ComponentShutdown args)
        {
            _actionsSystem.RemoveAction(uid, component.WakeAction);

            var ev = new SleepStateChangedEvent(false);
            RaiseLocalEvent(uid, ev);
            _blindableSystem.UpdateIsBlind(uid);
        }

        private void OnSpeakAttempt(EntityUid uid, SleepingComponent component, SpeakAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnSeeAttempt(EntityUid uid, SleepingComponent component, CanSeeAttemptEvent args)
        {
            if (component.LifeStage <= ComponentLifeStage.Running)
                args.Cancel();
        }
    }
}


public sealed partial class SleepActionEvent : InstantActionEvent {}

public sealed partial class WakeActionEvent : InstantActionEvent {}

/// <summary>
/// Raised on an entity when they fall asleep or wake up.
/// </summary>
public sealed class SleepStateChangedEvent : EntityEventArgs
{
    public bool FellAsleep = false;

    public SleepStateChangedEvent(bool fellAsleep)
    {
        FellAsleep = fellAsleep;
    }
}

using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Movement.Components; // Frontier
using Content.Shared.Stunnable; // Frontier: wheelchair users can crawl

namespace Content.Shared.Traits.Assorted;

public sealed class LegsParalyzedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!; // Frontier: wheelchair users can crawl

    public override void Initialize()
    {
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegsParalyzedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegsParalyzedComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, UnbuckledEvent>(OnUnbuckled);
        SubscribeLocalEvent<LegsParalyzedComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
        //SubscribeLocalEvent<LegsParalyzedComponent, UpdateCanMoveEvent>(OnUpdateCanMoveEvent); // Frontier: wheelchair users can crawl
        SubscribeLocalEvent<LegsParalyzedComponent, StandUpAttemptEvent>(OnStandUpAttemptEvent, before: [typeof(SharedStunSystem)]); // Frontier: wheelchair users can crawl
        SubscribeLocalEvent<LegsParalyzedComponent, StandAttemptEvent>(OnStandAttemptEvent, before: [typeof(SharedStunSystem)]); // Frontier: wheelchair users can crawl
        SubscribeLocalEvent<LegsParalyzedComponent, KnockedDownAlertEvent>(OnKnockedDownAlertEvent, before: [typeof(SharedStunSystem)]); // Frontier: wheelchair users can crawl
    }

    private void OnStartup(EntityUid uid, LegsParalyzedComponent component, ComponentStartup args)
    {
        // TODO: In future probably must be surgery related wound
        _movementSpeedModifierSystem.ChangeBaseSpeed(uid, 1.5f, 2.5f, MovementSpeedModifierComponent.DefaultAcceleration); // Frontier: wheelchair users can crawl
    }

    private void OnShutdown(EntityUid uid, LegsParalyzedComponent component, ComponentShutdown args)
    {
        _standingSystem.Stand(uid);
        _bodySystem.UpdateMovementSpeed(uid);
    }

    private void OnBuckled(EntityUid uid, LegsParalyzedComponent component, ref BuckledEvent args)
    {
        _standingSystem.Stand(uid);
        _stunSystem.SetAutoStand(uid, true); // Frontier: wheelchair users can crawl
    }

    private void OnUnbuckled(EntityUid uid, LegsParalyzedComponent component, ref UnbuckledEvent args)
    {
        _standingSystem.Down(uid, true, false, true); // Frontier: wheelchair users can crawl
        _stunSystem.TryCrawling(uid, null, false, false, false, true); // Frontier: wheelchair users can crawl
    }

    // Start Frontier: wheelchair users can crawl
    // private void OnUpdateCanMoveEvent(EntityUid uid, LegsParalyzedComponent component, UpdateCanMoveEvent args)
    // {
    //     if (HasComp<RelayInputMoverComponent>(uid)) // Frontier: allow relaying input with paralyzed legs
    //         return; // Frontier: allow relaying input with paralyzed legs
    //
    //     args.Cancel();
    // }

    private void OnStandUpAttemptEvent(EntityUid uid, LegsParalyzedComponent component, StandUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;
        args.Autostand = false;
    }

    private void OnStandAttemptEvent(EntityUid uid, LegsParalyzedComponent component, StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnKnockedDownAlertEvent(EntityUid uid, LegsParalyzedComponent component, KnockedDownAlertEvent args)
    {
        args.Handled = true;
    }
    // End Frontier: wheelchair users can crawl

    private void OnThrowPushbackAttempt(EntityUid uid, LegsParalyzedComponent component, ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }
}

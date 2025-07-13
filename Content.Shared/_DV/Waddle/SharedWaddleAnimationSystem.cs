using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Gravity;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared._DV.Waddle;

public abstract class SharedWaddleAnimationSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        // Startup
        SubscribeLocalEvent<WaddleAnimationComponent, ComponentStartup>(OnComponentStartup);

        // Start moving possibilities
        SubscribeLocalEvent<WaddleAnimationComponent, MoveInputEvent>(OnMovementInput);
        SubscribeLocalEvent<WaddleAnimationComponent, StoodEvent>(OnStood);

        // Stop moving possibilities
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref StunnedEvent _) => StopWaddling(ent));
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref DownedEvent _) => StopWaddling(ent));
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref BuckledEvent _) => StopWaddling(ent));
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref MobStateChangedEvent _) => StopWaddling(ent));
        SubscribeLocalEvent<WaddleAnimationComponent, GravityChangedEvent>(OnGravityChanged);
    }

    private void OnGravityChanged(Entity<WaddleAnimationComponent> ent, ref GravityChangedEvent args)
    {
        if (!args.HasGravity)
            StopWaddling(ent);
    }

    private void OnComponentStartup(Entity<WaddleAnimationComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<InputMoverComponent>(ent, out var mover))
            return;

        // If the waddler is currently moving, make them start waddling
        if ((mover.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None)
            SetWaddling(ent, true);
    }

    private void OnMovementInput(Entity<WaddleAnimationComponent> ent, ref MoveInputEvent args)
    {
        // Only start waddling if we're actually moving.
        SetWaddling(ent, args.HasDirectionalMovement);
    }

    private void OnStood(Entity<WaddleAnimationComponent> ent, ref StoodEvent args)
    {
        if (!TryComp<InputMoverComponent>(ent, out var mover))
            return;

        // only resume waddling if they are trying to move
        if ((mover.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            return;

        SetWaddling(ent, true);
    }

    private void StopWaddling(Entity<WaddleAnimationComponent> ent)
    {
        SetWaddling(ent, false);
    }

    /// <summary>
    /// Enables or disables waddling for a entity, including the animation.
    /// Unless force is true, prevents dead people etc from waddling using <see cref="CanWaddle"/>.
    /// </summary>
    private void SetWaddling(Entity<WaddleAnimationComponent> ent, bool waddling, bool force = false) // imp edit, made private
    {
        // it makes your sprite rotation stutter when moving, bad
        if (!_timing.IsFirstTimePredicted)
            return;

        if (waddling && !force && !CanWaddle(ent))
            waddling = false;

        if (ent.Comp.IsWaddling == waddling)
            return;

        ent.Comp.IsWaddling = waddling;
        DirtyField(ent, ent.Comp, nameof(WaddleAnimationComponent.IsWaddling));
        UpdateAnimation(ent);
    }

    /// <summary>
    /// Returns true if an entity is allowed to waddle at all.
    /// </summary>
    private bool CanWaddle(EntityUid uid) // imp edit, made private
    {
        // can't waddle when dead
        return _mob.IsAlive(uid) &&
            // bouncy shoes should make you spin in 0G really but definitely not bounce up and down
            !_gravity.IsWeightless(uid) &&
            // can't waddle if your legs are broken etc
            _actionBlocker.CanMove(uid) &&
            // can't waddle when buckled, if you are really strong/on meth the chair/bed should waddle instead
            !_buckle.IsBuckled(uid) &&
            // animation doesn't take being downed into account :(
            !_standing.IsDown(uid) &&
            // can't waddle in space... 1984
            Transform(uid).GridUid != null;
    }

    /// <summary>
    /// Updates the waddling animation on the client.
    /// Does nothing on server.
    /// </summary>
    protected virtual void UpdateAnimation(Entity<WaddleAnimationComponent> ent)
    {
    }
}

using System.Numerics;
using Content.Server.Body.Systems;
using Content.Server.Climbing.Components;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Server.Climbing;

[UsedImplicitly]
public sealed class ClimbSystem : SharedClimbSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private const string ClimbingFixtureName = "climb";
    private const int ClimbingCollisionGroup = (int) (CollisionGroup.TableLayer | CollisionGroup.LowImpassable);

    private readonly Dictionary<EntityUid, Dictionary<string, Fixture>> _fixtureRemoveQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<ClimbableComponent, GetVerbsEvent<AlternativeVerb>>(AddClimbableVerb);
        SubscribeLocalEvent<ClimbableComponent, DragDropTargetEvent>(OnClimbableDragDrop);

        SubscribeLocalEvent<ClimbingComponent, ClimbDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ClimbingComponent, EndCollideEvent>(OnClimbEndCollide);
        SubscribeLocalEvent<ClimbingComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<ClimbingComponent, ComponentGetState>(OnClimbingGetState);

        SubscribeLocalEvent<GlassTableComponent, ClimbedOnEvent>(OnGlassClimbed);
    }

    protected override void OnCanDragDropOn(EntityUid uid, ClimbableComponent component, ref CanDropTargetEvent args)
    {
        base.OnCanDragDropOn(uid, component, ref args);

        if (!args.CanDrop)
            return;

        string reason;
        var canVault = args.User == args.Dragged
            ? CanVault(component, args.User, uid, out reason)
            : CanVault(component, args.User, args.Dragged, uid, out reason);

        if (!canVault)
            _popupSystem.PopupEntity(reason, args.User, args.User);

        args.CanDrop = canVault;
        args.Handled = true;
    }

    private void AddClimbableVerb(EntityUid uid, ClimbableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanMove(args.User))
            return;

        if (!TryComp(args.User, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing)
            return;

        // TODO VERBS ICON add a climbing icon?
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryClimb(args.User, args.User, args.Target, out _, component),
            Text = Loc.GetString("comp-climbable-verb-climb")
        });
    }

    private void OnClimbableDragDrop(EntityUid uid, ClimbableComponent component, ref DragDropTargetEvent args)
    {
        // definitely a better way to check if two entities are equal
        // but don't have computer access and i have to do this without syntax
        if (args.Handled || args.User != args.Dragged && !HasComp<HandsComponent>(args.User))
            return;
        TryClimb(args.User, args.Dragged, uid, out _, component);
    }

    public bool TryClimb(EntityUid user,
        EntityUid entityToMove,
        EntityUid climbable,
        out DoAfterId? id,
        ClimbableComponent? comp = null,
        ClimbingComponent? climbing = null)
    {
        id = null;

        if (!Resolve(climbable, ref comp) || !Resolve(entityToMove, ref climbing))
            return false;

        // Note, IsClimbing does not mean a DoAfter is active, it means the target has already finished a DoAfter and
        // is currently on top of something..
        if (climbing.IsClimbing)
            return true;

        var args = new DoAfterArgs(EntityManager, user, comp.ClimbDelay, new ClimbDoAfterEvent(), entityToMove, target: climbable, used: entityToMove)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true
        };

        _audio.PlayPvs(comp.StartClimbSound, climbable);
        _doAfterSystem.TryStartDoAfter(args, out id);
        return true;
    }

    private void OnDoAfter(EntityUid uid, ClimbingComponent component, ClimbDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        Climb(uid, args.Args.User, args.Args.Used.Value, args.Args.Target.Value, climbing: component);

        args.Handled = true;
    }

    private void Climb(EntityUid uid, EntityUid user, EntityUid instigator, EntityUid climbable, bool silent = false, ClimbingComponent? climbing = null,
        PhysicsComponent? physics = null, FixturesComponent? fixtures = null, ClimbableComponent? comp = null)
    {
        if (!Resolve(uid, ref climbing, ref physics, ref fixtures, false))
            return;

        if (!Resolve(climbable, ref comp))
            return;

        if (!ReplaceFixtures(climbing, fixtures))
            return;

        climbing.IsClimbing = true;
        Dirty(climbing);

        _audio.PlayPvs(comp.FinishClimbSound, climbable);
        MoveEntityToward(uid, climbable, physics, climbing);
        // we may potentially need additional logic since we're forcing a player onto a climbable
        // there's also the cases where the user might collide with the person they are forcing onto the climbable that i haven't accounted for

        RaiseLocalEvent(uid, new StartClimbEvent(climbable), false);
        RaiseLocalEvent(climbable, new ClimbedOnEvent(uid, user), false);

        if (silent)
            return;
        if (user == uid)
        {
            var othersMessage = Loc.GetString("comp-climbable-user-climbs-other", ("user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));
            uid.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", climbable));
            uid.PopupMessage(selfMessage);
        }
        else
        {
            var othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other", ("user", Identity.Entity(user, EntityManager)),
                ("moved-user", Identity.Entity(uid, EntityManager)), ("climbable", climbable));
            user.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));
            user.PopupMessage(selfMessage);
        }
    }

    /// <summary>
    /// Replaces the current fixtures with non-climbing collidable versions so that climb end can be detected
    /// </summary>
    /// <returns>Returns whether adding the new fixtures was successful</returns>
    private bool ReplaceFixtures(ClimbingComponent climbingComp, FixturesComponent fixturesComp)
    {
        var uid = climbingComp.Owner;

        // Swap fixtures
        foreach (var (name, fixture) in fixturesComp.Fixtures)
        {
            if (climbingComp.DisabledFixtureMasks.ContainsKey(name)
                || fixture.Hard == false
                || (fixture.CollisionMask & ClimbingCollisionGroup) == 0)
                continue;

            climbingComp.DisabledFixtureMasks.Add(name, fixture.CollisionMask & ClimbingCollisionGroup);
            _physics.SetCollisionMask(uid, name, fixture, fixture.CollisionMask & ~ClimbingCollisionGroup, fixturesComp);
        }

        if (!_fixtureSystem.TryCreateFixture(
                uid,
                new PhysShapeCircle(0.35f),
                ClimbingFixtureName,
                collisionLayer: (int) CollisionGroup.None,
                collisionMask: ClimbingCollisionGroup,
                hard: false,
                manager: fixturesComp))
        {
            return false;
        }

        return true;
    }

    private void OnClimbEndCollide(EntityUid uid, ClimbingComponent component, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != ClimbingFixtureName
            || !component.IsClimbing
            || component.OwnerIsTransitioning)
            return;

        foreach (var fixture in args.OurFixture.Contacts.Keys)
        {
            if (fixture == args.OtherFixture)
                continue;
            // If still colliding with a climbable, do not stop climbing
            if (HasComp<ClimbableComponent>(args.OtherEntity))
                return;
        }

        StopClimb(uid, component);
    }

    private void StopClimb(EntityUid uid, ClimbingComponent? climbing = null, FixturesComponent? fixtures = null)
    {
        if (!Resolve(uid, ref climbing, ref fixtures, false))
            return;

        foreach (var (name, fixtureMask) in climbing.DisabledFixtureMasks)
        {
            if (!fixtures.Fixtures.TryGetValue(name, out var fixture))
            {
                continue;
            }

            _physics.SetCollisionMask(uid, name, fixture, fixture.CollisionMask | fixtureMask, fixtures);
        }
        climbing.DisabledFixtureMasks.Clear();

        if (!_fixtureRemoveQueue.TryGetValue(uid, out var removeQueue))
        {
            removeQueue = new Dictionary<string, Fixture>();
            _fixtureRemoveQueue.Add(uid, removeQueue);
        }

        if (fixtures.Fixtures.TryGetValue(ClimbingFixtureName, out var climbingFixture))
            removeQueue.Add(ClimbingFixtureName, climbingFixture);

        climbing.IsClimbing = false;
        climbing.OwnerIsTransitioning = false;
        var ev = new EndClimbEvent();
        RaiseLocalEvent(uid, ref ev);
        Dirty(climbing);
    }

    /// <summary>
    ///     Checks if the user can vault the target
    /// </summary>
    /// <param name="component">The component of the entity that is being vaulted</param>
    /// <param name="user">The entity that wants to vault</param>
    /// <param name="target">The object that is being vaulted</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    /// <returns></returns>
    public bool CanVault(ClimbableComponent component, EntityUid user, EntityUid target, out string reason)
    {
        if (!_actionBlockerSystem.CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!HasComp<ClimbingComponent>(user)
            || !TryComp(user, out BodyComponent? body)
            || !_bodySystem.BodyHasChildOfType(user, BodyPartType.Leg, body)
            || !_bodySystem.BodyHasChildOfType(user, BodyPartType.Foot, body))
        {
            reason = Loc.GetString("comp-climbable-cant-climb");
            return false;
        }

        if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range))
        {
            reason = Loc.GetString("comp-climbable-cant-reach");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    ///     Checks if the user can vault the dragged entity onto the the target
    /// </summary>
    /// <param name="component">The climbable component of the object being vaulted onto</param>
    /// <param name="user">The user that wants to vault the entity</param>
    /// <param name="dragged">The entity that is being vaulted</param>
    /// <param name="target">The object that is being vaulted onto</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    /// <returns></returns>
    public bool CanVault(ClimbableComponent component, EntityUid user, EntityUid dragged, EntityUid target,
        out string reason)
    {
        if (!_actionBlockerSystem.CanInteract(user, dragged) || !_actionBlockerSystem.CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!HasComp<ClimbingComponent>(dragged))
        {
            reason = Loc.GetString("comp-climbable-cant-climb");
            return false;
        }

        bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

        if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range, predicate: Ignored)
            || !_interactionSystem.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored))
        {
            reason = Loc.GetString("comp-climbable-cant-reach");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public void ForciblySetClimbing(EntityUid uid, EntityUid climbable, ClimbingComponent? component = null)
    {
        Climb(uid, uid, uid, climbable, true, component);
    }

    private void OnBuckleChange(EntityUid uid, ClimbingComponent component, ref BuckleChangeEvent args)
    {
        if (!args.Buckling)
            return;
        StopClimb(uid, component);
    }

    private static void OnClimbingGetState(EntityUid uid, ClimbingComponent component, ref ComponentGetState args)
    {
        args.State = new ClimbingComponent.ClimbModeComponentState(component.IsClimbing, component.OwnerIsTransitioning);
    }

    private void OnGlassClimbed(EntityUid uid, GlassTableComponent component, ClimbedOnEvent args)
    {
        if (TryComp<PhysicsComponent>(args.Climber, out var physics) && physics.Mass <= component.MassLimit)
            return;

        _damageableSystem.TryChangeDamage(args.Climber, component.ClimberDamage, origin: args.Climber);
        _damageableSystem.TryChangeDamage(uid, component.TableDamage, origin: args.Climber);
        _stunSystem.TryParalyze(args.Climber, TimeSpan.FromSeconds(component.StunTime), true);

        // Not shown to the user, since they already get a 'you climb on the glass table' popup
        _popupSystem.PopupEntity(
            Loc.GetString("glass-table-shattered-others", ("table", uid), ("climber", Identity.Entity(args.Climber, EntityManager))), args.Climber,
            Filter.PvsExcept(args.Climber), true);
    }

    /// <summary>
    /// Moves the entity toward the target climbed entity
    /// </summary>
    public void MoveEntityToward(EntityUid uid, EntityUid target, PhysicsComponent? physics = null, ClimbingComponent? climbing = null)
    {
        if (!Resolve(uid, ref physics, ref climbing, false))
            return;

        var from = Transform(uid).WorldPosition;
        var to = Transform(target).WorldPosition;
        var (x, y) = (to - from).Normalized();

        if (MathF.Abs(x) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
            to = new Vector2(from.X, to.Y);
        else if (MathF.Abs(y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
            to = new Vector2(to.X, from.Y);

        var velocity = (to - from).Length();

        if (velocity <= 0.0f)
            return;

        // Since there are bodies with different masses:
        // mass * 10 seems enough to move entity
        // instead of launching cats like rockets against the walls with constant impulse value.
        _physics.ApplyLinearImpulse(uid, (to - from).Normalized() * velocity * physics.Mass * 10, body: physics);
        _physics.SetBodyType(uid, BodyType.Dynamic, body: physics);
        climbing.OwnerIsTransitioning = true;
        _actionBlockerSystem.UpdateCanMove(uid);

        // Transition back to KinematicController after BufferTime
        climbing.Owner.SpawnTimer((int) (ClimbingComponent.BufferTime * 1000), () =>
        {
            if (climbing.Deleted)
                return;

            _physics.SetBodyType(uid, BodyType.KinematicController);
            climbing.OwnerIsTransitioning = false;
            _actionBlockerSystem.UpdateCanMove(uid);
        });
    }

    public override void Update(float frameTime)
    {
        foreach (var (uid, fixtures) in _fixtureRemoveQueue)
        {
            if (!TryComp<PhysicsComponent>(uid, out var physicsComp)
                || !TryComp<FixturesComponent>(uid, out var fixturesComp))
            {
                continue;
            }

            foreach (var fixture in fixtures)
            {
                _fixtureSystem.DestroyFixture(uid, fixture.Key, fixture.Value, body: physicsComp, manager: fixturesComp);
            }
        }

        _fixtureRemoveQueue.Clear();
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        _fixtureRemoveQueue.Clear();
    }

}

/// <summary>
///     Raised on an entity when it is climbed on.
/// </summary>
public sealed class ClimbedOnEvent : EntityEventArgs
{
    public EntityUid Climber;
    public EntityUid Instigator;

    public ClimbedOnEvent(EntityUid climber, EntityUid instigator)
    {
        Climber = climber;
        Instigator = instigator;
    }
}

/// <summary>
///     Raised on an entity when it successfully climbs on something.
/// </summary>
public sealed class StartClimbEvent : EntityEventArgs
{
    public EntityUid Climbable;

    public StartClimbEvent(EntityUid climbable)
    {
        Climbable = climbable;
    }
}

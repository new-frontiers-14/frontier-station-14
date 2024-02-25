using System.Numerics;
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Resist;
using Content.Server.Popups;
using Content.Server.Contests;
using Content.Server.Item.PseudoItem;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Climbing; // Shared instead of Server
using Content.Shared.Mobs;
using Content.Shared.DoAfter;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Stunnable;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Climbing.Events; // Added this.
using Content.Shared.Carrying;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.ActionBlocker;
using Content.Shared.Item;
using Content.Shared.Item.PseudoItem;
using Content.Shared.Mind.Components;
using Content.Shared.Throwing;
using Content.Shared.Physics.Pull;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Map.Components;

namespace Content.Server.Carrying
{
    public sealed class CarryingSystem : EntitySystem
    {
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly StandingStateSystem _standingState = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedPullingSystem _pullingSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly EscapeInventorySystem _escapeInventorySystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ContestsSystem _contests = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly PseudoItemSystem _pseudoItem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
            SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnThrow);
            SubscribeLocalEvent<CarryingComponent, EntParentChangedMessage>(OnParentChanged);
            SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<BeingCarriedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, MoveInputEvent>(OnMoveInput);
            SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
            SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnStartClimb);
            SubscribeLocalEvent<BeingCarriedComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<CarryingComponent, GetVerbsEvent<InnateVerb>>(AddInsertCarriedVerb); // Frontier
        }


        private void AddCarryVerb(EntityUid uid, CarriableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!CanCarry(args.User, uid, component))
                return;

            if (HasComp<CarryingComponent>(args.User)) // yeah not dealing with that
                return;

            if (HasComp<BeingCarriedComponent>(args.User) || HasComp<BeingCarriedComponent>(args.Target))
                return;

            if (!_mobStateSystem.IsAlive(args.User))
                return;

            if (args.User == args.Target)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartCarryDoAfter(args.User, uid, component);
                },
                Text = Loc.GetString("carry-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        /// <summary>
        /// Since the carried entity is stored as 2 virtual items, when deleted we want to drop them.
        /// </summary>
        private void OnVirtualItemDeleted(EntityUid uid, CarryingComponent component, VirtualItemDeletedEvent args)
        {
            if (!HasComp<CarriableComponent>(args.BlockingEntity))
                return;

            DropCarried(uid, args.BlockingEntity);
        }

        /// <summary>
        /// Basically using virtual item passthrough to throw the carried person. A new age!
        /// Maybe other things besides throwing should use virt items like this...
        /// </summary>
        private void OnThrow(EntityUid uid, CarryingComponent component, BeforeThrowEvent args)
        {
            if (!TryComp<HandVirtualItemComponent>(args.ItemUid, out var virtItem) || !HasComp<CarriableComponent>(virtItem.BlockingEntity))
                return;

            args.ItemUid = virtItem.BlockingEntity;

            var multiplier = _contests.MassContest(uid, virtItem.BlockingEntity);
            args.ThrowStrength = 5f * multiplier;
        }

        private void OnParentChanged(EntityUid uid, CarryingComponent component, ref EntParentChangedMessage args)
        {
            var xform = Transform(uid);
            if (xform.MapID != args.OldMapId)
                return;

            // Do not drop the carried entity if the new parent is a grid
            if (xform.ParentUid == xform.GridUid)
                return;

            DropCarried(uid, component.Carried);
        }

        private void OnMobStateChanged(EntityUid uid, CarryingComponent component, MobStateChangedEvent args)
        {
            DropCarried(uid, component.Carried);
        }

        /// <summary>
        /// Only let the person being carried interact with their carrier and things on their person.
        /// </summary>
        private void OnInteractionAttempt(EntityUid uid, BeingCarriedComponent component, InteractionAttemptEvent args)
        {
            if (args.Target == null)
                return;

            var targetParent = Transform(args.Target.Value).ParentUid;

            if (args.Target.Value != component.Carrier && targetParent != component.Carrier && targetParent != uid)
                args.Cancel();
        }

        /// <summary>
        /// Try to escape via the escape inventory system.
        /// </summary>
        private void OnMoveInput(EntityUid uid, BeingCarriedComponent component, ref MoveInputEvent args)
        {
            if (!TryComp<CanEscapeInventoryComponent>(uid, out var escape))
                return;

            if (args.OldMovement == MoveButtons.None || args.OldMovement == MoveButtons.Walk)
                return; // Don't try to escape if not moving *cries*

            if (_actionBlockerSystem.CanInteract(uid, component.Carrier))
            {
                _escapeInventorySystem.AttemptEscape(uid, component.Carrier, escape, _contests.MassContest(uid, component.Carrier));
            }
        }

        private void OnMoveAttempt(EntityUid uid, BeingCarriedComponent component, UpdateCanMoveEvent args)
        {
            args.Cancel();
        }

        private void OnStandAttempt(EntityUid uid, BeingCarriedComponent component, StandAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInteractedWith(EntityUid uid, BeingCarriedComponent component, GettingInteractedWithAttemptEvent args)
        {
            if (args.Uid != component.Carrier)
                args.Cancel();
        }

        private void OnPullAttempt(EntityUid uid, BeingCarriedComponent component, PullAttemptEvent args)
        {
            args.Cancelled = true;
        }

        private void OnStartClimb(EntityUid uid, BeingCarriedComponent component, ref StartClimbEvent args)
        {
            DropCarried(component.Carrier, uid);
        }

        private void OnBuckleChange(EntityUid uid, BeingCarriedComponent component, ref BuckleChangeEvent args)
        {
            DropCarried(component.Carrier, uid);
        }

        private void OnDoAfter(EntityUid uid, CarriableComponent component, CarryDoAfterEvent args)
        {
            component.CancelToken = null;
            if (args.Handled || args.Cancelled)
                return;

            if (!CanCarry(args.Args.User, uid, component))
                return;

            Carry(args.Args.User, uid);
            args.Handled = true;
        }

        private void StartCarryDoAfter(EntityUid carrier, EntityUid carried, CarriableComponent component)
        {
            var length = GetPickupDuration(carrier, carried); // Frontier: instead of in-line calculation, use a separate function
            if (length >= TimeSpan.FromSeconds(9))
            {
                _popupSystem.PopupEntity(Loc.GetString("carry-too-heavy"), carried, carrier, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (!HasComp<KnockedDownComponent>(carried))
                length *= 2f;

            component.CancelToken = new CancellationTokenSource();

            var ev = new CarryDoAfterEvent();
            var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            };
            _doAfterSystem.TryStartDoAfter(args);

            _popupSystem.PopupEntity(Loc.GetString("carry-started", ("carrier", carrier)), carried, carried);
        }

        private void Carry(EntityUid carrier, EntityUid carried)
        {
            if (TryComp<SharedPullableComponent>(carried, out var pullable))
                _pullingSystem.TryStopPull(pullable);

            // Don't allow people to stack upon each other. They're too weak for that!
            if (TryComp<CarryingComponent>(carried, out var carryComp))
                DropCarried(carried, carryComp.Carried);

            Transform(carrier).AttachToGridOrMap();
            Transform(carried).AttachToGridOrMap();
            Transform(carried).Coordinates = Transform(carrier).Coordinates;
            Transform(carried).AttachParent(Transform(carrier));
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            var carryingComp = EnsureComp<CarryingComponent>(carrier);
            ApplyCarrySlowdown(carrier, carried);
            var carriedComp = EnsureComp<BeingCarriedComponent>(carried);
            EnsureComp<KnockedDownComponent>(carried);

            carryingComp.Carried = carried;
            carriedComp.Carrier = carrier;

            _actionBlockerSystem.UpdateCanMove(carried);
        }

        public void DropCarried(EntityUid carrier, EntityUid carried)
        {
            RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
            RemComp<CarryingSlowdownComponent>(carrier);
            RemComp<BeingCarriedComponent>(carried);
            RemComp<KnockedDownComponent>(carried);
            _actionBlockerSystem.UpdateCanMove(carried);
            _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
            Transform(carried).AttachToGridOrMap();
            _standingState.Stand(carried);
            _movementSpeed.RefreshMovementSpeedModifiers(carrier);
        }

        private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
        {
            var massRatio = _contests.MassContest(carrier, carried);

            if (massRatio == 0)
                massRatio = 1;

            var massRatioSq = Math.Pow(massRatio, 2);
            var modifier = (1 - (0.15 / massRatioSq));
            modifier = Math.Max(0.1, modifier);
            var slowdownComp = EnsureComp<CarryingSlowdownComponent>(carrier);
            _slowdown.SetModifier(carrier, (float) modifier, (float) modifier, slowdownComp);
        }

        public bool CanCarry(EntityUid carrier, EntityUid carried, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(carried, ref carriedComp, false))
                return false;

            if (carriedComp.CancelToken != null)
                return false;

            if (!HasComp<MapGridComponent>(Transform(carrier).ParentUid))
                return false;

            if (HasComp<BeingCarriedComponent>(carrier) || HasComp<BeingCarriedComponent>(carried))
                return false;

        //  if (_respirator.IsReceivingCPR(carried))
            //  return false;

            if (!TryComp<HandsComponent>(carrier, out var hands))
                return false;

            if (hands.CountFreeHands() < carriedComp.FreeHandsRequired)
                return false;

            return true;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BeingCarriedComponent>();
            while (query.MoveNext(out var carried, out var comp))
            {
                var carrier = comp.Carrier;
                if (carrier is not { Valid: true } || carried is not { Valid: true })
                    continue;

                // Make sure the carried entity is always centered relative to the carrier, as gravity pulls can offset it otherwise
                var xform = Transform(carried);
                if (!xform.LocalPosition.EqualsApprox(Vector2.Zero))
                {
                    xform.LocalPosition = Vector2.Zero;
                }
            }
            query.Dispose();
        }

        public TimeSpan GetPickupDuration(EntityUid carrier, EntityUid carried)
        {
            TimeSpan length = TimeSpan.FromSeconds(3);

            var mod = _contests.MassContest(carrier, carried);
            if (mod != 0)
                length /= mod;

            return length;
        }

        public bool TryCarry(EntityUid carrier, EntityUid toCarry, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(toCarry, ref carriedComp, false))
                return false;

            if (!CanCarry(carrier, toCarry, carriedComp))
                return false;

            // The second one means that carrier *is also* inside a bag.
            if (HasComp<BeingCarriedComponent>(carrier) || HasComp<ItemComponent>(carrier))
                return false;

            if (GetPickupDuration(carrier, toCarry) > TimeSpan.FromSeconds(9))
                return false;

            Carry(carrier, toCarry);

            return true;
        }

        private void AddInsertCarriedVerb(EntityUid uid, CarryingComponent component, GetVerbsEvent<InnateVerb> args)
        {
            var toInsert = args.Using;
            if (toInsert is not { Valid: true } || !args.CanAccess || !TryComp<PseudoItemComponent>(toInsert, out var pseudoItem))
                return;

            if (!HasComp<StorageComponent>(args.Target))
                return; // Can't check if the person would actually fit here

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    DropCarried(uid, toInsert.Value);
                    _pseudoItem.TryInsert(args.Target, toInsert.Value, args.User, pseudoItem);
                },
                Text = Loc.GetString("action-name-insert-other", ("target", toInsert)),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }
}

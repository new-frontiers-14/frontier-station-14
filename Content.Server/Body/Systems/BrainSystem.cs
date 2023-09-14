using Content.Server.Body.Components;
using Content.Server.Ghost.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Body.Systems
{
    public sealed class BrainSystem : EntitySystem
    {
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, AddedToBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            SubscribeLocalEvent<BrainComponent, AddedToPartEvent>((uid, _, args) => HandleMind(args.Part, uid));
            SubscribeLocalEvent<BrainComponent, AddedToPartInBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            SubscribeLocalEvent<BrainComponent, RemovedFromBodyEvent>(OnRemovedFromBody);
            SubscribeLocalEvent<BrainComponent, RemovedFromPartEvent>((uid, _, args) => HandleMind(uid, args.Old));
            SubscribeLocalEvent<BrainComponent, RemovedFromPartInBodyEvent>((uid, _, args) => HandleMind(args.OldBody, uid));
        }

        private void OnRemovedFromBody(EntityUid uid, BrainComponent component, RemovedFromBodyEvent args)
        {
            // This one needs to be special, okay?
            if (!EntityManager.TryGetComponent(uid, out OrganComponent? organ) ||
                organ.ParentSlot is not {Parent: var parent})
                return;

            HandleMind(parent, args.Old);
        }

        private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
        {
            EnsureComp<MindContainerComponent>(newEntity);
            var oldMind = EnsureComp<MindContainerComponent>(oldEntity);

            var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
            if (HasComp<BodyComponent>(newEntity))
                ghostOnMove.MustBeDead = true;

            // TODO: This is an awful solution.
            // Our greatest minds still can't figure out how to allow brains/heads to ghost without giving them the
            // ability to move first. I hate this with a passion.
            if (!HasComp<InputMoverComponent>(newEntity))
            {
                AddComp<InputMoverComponent>(newEntity);
                var move = EnsureComp<MovementSpeedModifierComponent>(newEntity);
                _movementSpeed.ChangeBaseSpeed(newEntity, 0, 0 , 0, move);
            }

            if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
                return;

            _mindSystem.TransferTo(mindId, newEntity, mind: mind);
        }
    }
}

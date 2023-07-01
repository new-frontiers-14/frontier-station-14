using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Stacks;

namespace Content.Server.Stack
{
    public sealed class StackHolderSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StackHolderComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StackHolderComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnExamined(EntityUid uid, StackHolderComponent component, ExaminedEvent args)
        {
            var item = _itemSlotsSystem.GetItemOrNull(uid, "stack_slot");

            if (item == null)
            {
                args.PushMarkup(Loc.GetString("stack-holder-empty"));
                return;
            }

            if (TryComp<StackComponent>(item, out var stack))
            {
                args.PushMarkup(Loc.GetString("stack-holder", ("number", stack.Count), ("item", item)));
            }
        }

        private void OnAfterInteract(EntityUid uid, StackHolderComponent component, AfterInteractEvent args)
        {
            var item = _itemSlotsSystem.GetItemOrNull(uid, "stack_slot");
            if (item == null)
            {
                if (args.Target != null)
                    _itemSlotsSystem.TryInsert(uid, "stack_slot", args.Target.Value, args.User);
                return;
            }
            var afterEv = new AfterInteractEvent(args.User, (EntityUid) item, args.Target, args.ClickLocation, args.CanReach);
            RaiseLocalEvent((EntityUid) item, afterEv, false);
            if (args.Target != null)
            {
                var ev = new InteractUsingEvent(args.User, (EntityUid) item, args.Target.Value, args.ClickLocation);
                RaiseLocalEvent(args.Target.Value, ev, false);
            }
        }
    }
}

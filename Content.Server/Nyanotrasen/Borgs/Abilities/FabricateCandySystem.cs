using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server.Borgs.Abilities
{
    public sealed class FabricateCandySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FabricateCandyComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FabricateLollipopActionEvent>(OnLollipop);
            SubscribeLocalEvent<FabricateGumballActionEvent>(OnGumball);
        }

        private void OnInit(EntityUid uid, FabricateCandyComponent component, ComponentInit args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("FabricateLollipop", out var lollipop))
                _actions.AddAction(uid, new InstantAction(lollipop), null);

            if (_prototypeManager.TryIndex<InstantActionPrototype>("FabricateGumball", out var gumball))
                _actions.AddAction(uid, new InstantAction(gumball), null);
        }

        private void OnLollipop(FabricateLollipopActionEvent args)
        {
            Spawn("FoodLollipop", Transform(args.Performer).Coordinates);
            args.Handled = true;
        }

        private void OnGumball(FabricateGumballActionEvent args)
        {
            Spawn("FoodGumball", Transform(args.Performer).Coordinates);
            args.Handled = true;
        }
    }

    public sealed class FabricateLollipopActionEvent : InstantActionEvent {}
    public sealed class FabricateGumballActionEvent : InstantActionEvent {}
}

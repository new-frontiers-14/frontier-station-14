using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.Actions;
using Content.Shared.IdentityManagement;
using Content.Server.Body.Components;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nyanotrasen.Abilities;

namespace Content.Server.Abilities.Felinid
{
    public sealed class FelinidSystem : EntitySystem
    {

        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly VomitSystem _vomitSystem = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FelinidComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FelinidComponent, HairballActionEvent>(OnHairball);
            SubscribeLocalEvent<FelinidComponent, EatMouseActionEvent>(OnEatMouse);
            SubscribeLocalEvent<FelinidComponent, DidEquipHandEvent>(OnEquipped);
            SubscribeLocalEvent<FelinidComponent,DidUnequipHandEvent>(OnUnequipped);
            SubscribeLocalEvent<HairballComponent, ThrowDoHitEvent>(OnHairballHit);
            SubscribeLocalEvent<HairballComponent, GettingPickedUpAttemptEvent>(OnHairballPickupAttempt);
        }

        private Queue<EntityUid> RemQueue = new();

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var cat in RemQueue)
            {
                RemComp<CoughingUpHairballComponent>(cat);
            }
            RemQueue.Clear();

            foreach (var (hairballComp, catComp) in EntityQuery<CoughingUpHairballComponent, FelinidComponent>())
            {
                hairballComp.Accumulator += frameTime;
                if (hairballComp.Accumulator < hairballComp.CoughUpTime.TotalSeconds)
                    continue;

                hairballComp.Accumulator = 0;
                SpawnHairball(hairballComp.Owner, catComp);
                RemQueue.Enqueue(hairballComp.Owner);
            }
        }

        private void OnInit(EntityUid uid, FelinidComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<EntityPrototype>("ActionHairball", out var hairball))
                return;
            component.HairballAction = Spawn(hairball.ID);
            _actionsSystem.AddAction(uid, component.HairballAction.Value, null);
        }

        private void OnEquipped(EntityUid uid, FelinidComponent component, DidEquipHandEvent args)
        {
            if (!HasComp<FelinidFoodComponent>(args.Equipped))
                return;

            component.PotentialTarget = args.Equipped;

            if (!_prototypeManager.TryIndex<EntityPrototype>("ActionEatMouse", out var eatMouse))
                return;
            var actionId = Spawn(eatMouse.ID);
            _actionsSystem.AddAction(uid, actionId, null);
        }

        private void OnUnequipped(EntityUid uid, FelinidComponent component, DidUnequipHandEvent args)
        {
            if (args.Unequipped == component.PotentialTarget)
            {
                component.PotentialTarget = null;
                if (_prototypeManager.TryIndex<EntityPrototype>("ActionEatMouse", out var eatMouse))
                    _actionsSystem.RemoveAction(uid, eatMouse.ID);
            }
        }

        private void OnHairball(EntityUid uid, FelinidComponent component, HairballActionEvent args)
        {
            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
            EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
            blocker.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("hairball-mask", ("mask", maskUid)), uid, uid);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("hairball-cough", ("name", Identity.Entity(uid, EntityManager))), uid);
            SoundSystem.Play("/Audio/Effects/Species/hairball.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.15f));

            EnsureComp<CoughingUpHairballComponent>(uid);
            args.Handled = true;
        }

        private void OnEatMouse(EntityUid uid, FelinidComponent component, EatMouseActionEvent args)
        {
            if (component.PotentialTarget == null)
                return;

            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            if (hunger.CurrentThreshold == Shared.Nutrition.Components.HungerThreshold.Overfed)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more"), uid, uid, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
            EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
            blocker.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("hairball-mask", ("mask", maskUid)), uid, uid, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (component.HairballAction != null)
            {
                var actionData = _actionsSystem.GetActionData(component.HairballAction);
                _actionsSystem.SetCharges(component.HairballAction, actionData!.Charges + 1);
                _actionsSystem.SetEnabled(component.HairballAction, true);
            }
            Del(component.PotentialTarget.Value);
            component.PotentialTarget = null;

            SoundSystem.Play("/Audio/Items/eatfood.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.15f));

            _hunger.ModifyHunger(uid, 70f, hunger);
            if (_prototypeManager.TryIndex<EntityPrototype>("ActionEatMouse", out var eatMouse))
                    _actionsSystem.RemoveAction(uid, eatMouse.ID);
        }

        private void SpawnHairball(EntityUid uid, FelinidComponent component)
        {
            var hairball = EntityManager.SpawnEntity(component.HairballPrototype, Transform(uid).Coordinates);
            var hairballComp = Comp<HairballComponent>(hairball);

            if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
            {
                var temp = bloodstream.ChemicalSolution.SplitSolution(20);

                if (_solutionSystem.TryGetSolution(hairball, hairballComp.SolutionName, out var hairballSolution))
                {
                    _solutionSystem.TryAddSolution(hairball, hairballSolution, temp);
                }
            }
        }
        private void OnHairballHit(EntityUid uid, HairballComponent component, ThrowDoHitEvent args)
        {
            if (HasComp<FelinidComponent>(args.Target) || !HasComp<StatusEffectsComponent>(args.Target))
                return;
            if (_robustRandom.Prob(0.2f))
                _vomitSystem.Vomit(args.Target);
        }

        private void OnHairballPickupAttempt(EntityUid uid, HairballComponent component, GettingPickedUpAttemptEvent args)
        {
            if (HasComp<FelinidComponent>(args.User) || !HasComp<StatusEffectsComponent>(args.User))
                return;

            if (_robustRandom.Prob(0.2f))
            {
                _vomitSystem.Vomit(args.User);
                args.Cancel();
            }
        }
    }

}

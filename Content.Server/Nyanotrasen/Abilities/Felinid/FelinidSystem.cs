using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Medical;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nyanotrasen.Abilities;
using Content.Shared.CombatMode.Pacification; // Frontier

namespace Content.Server.Abilities.Felinid
{
    public sealed class FelinidSystem : EntitySystem
    {

        [Dependency] private readonly SharedAudioSystem _audio = default!;
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
            SubscribeLocalEvent<FelinidComponent, DidUnequipHandEvent>(OnUnequipped);
            SubscribeLocalEvent<HairballComponent, ThrowDoHitEvent>(OnHairballHit);
            SubscribeLocalEvent<HairballComponent, GettingPickedUpAttemptEvent>(OnHairballPickupAttempt);

            SubscribeLocalEvent<HairballComponent, AttemptPacifiedThrowEvent>(OnHairballAttemptPacifiedThrow); // Frontier - Block hairball abuse
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
            _actionsSystem.AddAction(uid, hairball.ID);
        }

        private void OnEquipped(EntityUid uid, FelinidComponent component, DidEquipHandEvent args)
        {
            if (!HasComp<FelinidFoodComponent>(args.Equipped))
                return;

            component.PotentialTarget = args.Equipped;

            if (!_prototypeManager.TryIndex<EntityPrototype>("ActionEatMouse", out var eatMouse))
                return;
            component.EatMouseAction = _actionsSystem.AddAction(uid, eatMouse.ID);
        }

        private void OnUnequipped(EntityUid uid, FelinidComponent component, DidUnequipHandEvent args)
        {
            if (args.Unequipped == component.PotentialTarget)
            {
                component.PotentialTarget = null;
                if (component.EatMouseAction != null)
                    _actionsSystem.RemoveAction(component.EatMouseAction);
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
            _audio.PlayPvs("/Audio/Nyanotrasen/Voice/Felinid/hairball.ogg", uid, AudioHelpers.WithVariation(0.15f));

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

            if (component.HairballAction != null
                && _actionsSystem.TryGetActionData(component.HairballAction, out var actionData))
            {
                _actionsSystem.SetCharges(component.HairballAction, actionData!.Charges + 1);
                _actionsSystem.SetEnabled(component.HairballAction, true);
            }
            Del(component.PotentialTarget.Value);
            component.PotentialTarget = null;

            _audio.PlayPvs("/Audio/Items/eatfood.ogg", uid, AudioHelpers.WithVariation(0.15f));

            _hunger.ModifyHunger(uid, 70f, hunger);
            _actionsSystem.RemoveAction(uid, component.EatMouseAction);
        }

        private void SpawnHairball(EntityUid uid, FelinidComponent component)
        {
            var hairball = EntityManager.SpawnEntity(component.HairballPrototype, Transform(uid).Coordinates);
            var hairballComp = Comp<HairballComponent>(hairball);

            if (TryComp<BloodstreamComponent>(uid, out var bloodstream) && bloodstream.ChemicalSolution is Entity<SolutionComponent> bloodSol)
            {
                var tempSol = _solutionSystem.SplitSolution(bloodSol, 20);

                if (_solutionSystem.TryGetSolution(hairball, hairballComp.SolutionName, out var hairballSolution)
                    && hairballSolution is Entity<SolutionComponent> solution)
                {
                    _solutionSystem.TryAddSolution(solution, tempSol);
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

        private void OnHairballAttemptPacifiedThrow(Entity<HairballComponent> ent, ref AttemptPacifiedThrowEvent args) // Frontier - Block hairball abuse
        {
            args.Cancel("pacified-cannot-throw-hairball");
        }
    }

}

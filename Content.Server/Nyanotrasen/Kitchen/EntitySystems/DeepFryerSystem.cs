using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Miasma;
using Content.Server.Audio;
using Content.Server.Body.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Kitchen.Components;
using Content.Server.NPC.Components;
using Content.Server.Nutrition;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Server.UserInterface;
using Content.Shared.Atmos.Miasma;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.UI;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Throwing;
using Content.Shared.Tools.Components;
using FastAccessors;
using Content.Shared.NPC;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction;
//using Robust.Shared.Audio.Systems;

namespace Content.Server.Kitchen.EntitySystems
{
    public sealed class DeepFryerSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
        [Dependency] private readonly IGameTiming _gameTimingSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SolutionTransferSystem _solutionTransferSystem = default!;
        [Dependency] private readonly PuddleSystem _puddleSystem = default!;
        [Dependency] private readonly TemperatureSystem _temperature = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

        private static readonly string CookingDamageType = "Heat";
        private static readonly float CookingDamageAmount = 10.0f;
        private static readonly float PvsWarningRange = 0.5f;
        private static readonly float ThrowMissChance = 0.25f;
        private static readonly int MaximumCrispiness = 2;
        private static readonly float BloodToProteinRatio = 0.1f;
        private static readonly string MobFlavorMeat = "meaty";
        private static readonly AudioParams AudioParamsInsertRemove = new(0.5f, 1f, "Master", 5f, 1.5f, 1f, false, 0f, 0.2f);

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("deepfryer");

            SubscribeLocalEvent<DeepFryerComponent, ComponentInit>(OnInitDeepFryer);
            SubscribeLocalEvent<DeepFryerComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<DeepFryerComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<DeepFryerComponent, MachineDeconstructedEvent>(OnDeconstruct);
            SubscribeLocalEvent<DeepFryerComponent, DestructionEventArgs>(OnDestruction);
            SubscribeLocalEvent<DeepFryerComponent, ThrowHitByEvent>(OnThrowHitBy);
            SubscribeLocalEvent<DeepFryerComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<DeepFryerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<DeepFryerComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeLocalEvent<DeepFryerComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
            SubscribeLocalEvent<DeepFryerComponent, DeepFryerRemoveItemMessage>(OnRemoveItem);
            SubscribeLocalEvent<DeepFryerComponent, DeepFryerInsertItemMessage>(OnInsertItem);
            SubscribeLocalEvent<DeepFryerComponent, DeepFryerScoopVatMessage>(OnScoopVat);
            SubscribeLocalEvent<DeepFryerComponent, DeepFryerClearSlagMessage>(OnClearSlagStart);
            SubscribeLocalEvent<DeepFryerComponent, DeepFryerRemoveAllItemsMessage>(OnRemoveAllItems);
            SubscribeLocalEvent<DeepFryerComponent, ClearSlagDoAfterEvent>(OnClearSlag);

            SubscribeLocalEvent<DeepFriedComponent, ComponentInit>(OnInitDeepFried);
            SubscribeLocalEvent<DeepFriedComponent, ExaminedEvent>(OnExamineFried);
            SubscribeLocalEvent<DeepFriedComponent, PriceCalculationEvent>(OnPriceCalculation);
            SubscribeLocalEvent<DeepFriedComponent, SliceFoodEvent>(OnSliceDeepFried);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in EntityManager.EntityQuery<DeepFryerComponent>())
            {
                var uid = component.Owner;

                if (_gameTimingSystem.CurTime < component.NextFryTime ||
                    !_powerReceiverSystem.IsPowered(uid))
                {
                    continue;
                }

                UpdateNextFryTime(uid, component);

                // Heat the vat solution and contained entities.
                _solutionContainerSystem.SetTemperature(uid, component.Solution, component.PoweredTemperature);

                foreach (var item in component.Storage.ContainedEntities)
                    CookItem(uid, component, item);

                // Do something bad if there's enough heat but not enough oil.
                var oilVolume = GetOilVolume(uid, component);

                if (oilVolume < component.SafeOilVolume)
                {
                    foreach (var item in component.Storage.ContainedEntities.ToArray())
                        BurnItem(uid, component, item);

                    if (oilVolume > FixedPoint2.Zero)
                    {
                        //JJ Comment - this code block makes the Linter fail, and doesn't seem to be necessary with the changes I made.
                        foreach (var reagent in component.Solution.Contents.ToArray())
                        {
                            _prototypeManager.TryIndex<ReagentPrototype>(reagent.Reagent.ToString(), out var proto);

                            foreach (var effect in component.UnsafeOilVolumeEffects)
                            {
                                effect.Effect(new ReagentEffectArgs(uid,
                                        null,
                                        component.Solution,
                                        proto!,
                                        reagent.Quantity,
                                        EntityManager,
                                        null,
                                        1f));
                            }

                        }

                        component.Solution.RemoveAllSolution();

                        _popupSystem.PopupEntity(
                            Loc.GetString("deep-fryer-oil-volume-low",
                                ("deepFryer", uid)),
                            uid,
                            PopupType.SmallCaution);

                        continue;
                    }
                }

                // We only alert the chef that there's a problem with oil purity
                // if there's anything to cook beyond this point.
                if (!component.Storage.ContainedEntities.Any())
                {
                    continue;
                }

                if (GetOilPurity(uid, component) < component.FryingOilThreshold)
                {
                    _popupSystem.PopupEntity(
                        Loc.GetString("deep-fryer-oil-purity-low",
                            ("deepFryer", uid)),
                        uid,
                        Filter.Pvs(uid, PvsWarningRange),
                        true);
                    continue;
                }

                foreach (var item in component.Storage.ContainedEntities.ToArray())
                    DeepFry(uid, component, item);

                // After the round of frying, replace the spent oil with a
                // waste product.
                if (component.WasteToAdd > FixedPoint2.Zero)
                {
                    foreach (var reagent in component.WasteReagents)
                        component.Solution.AddReagent(reagent.Reagent.ToString(), reagent.Quantity * component.WasteToAdd);

                    component.WasteToAdd = FixedPoint2.Zero;

                    _solutionContainerSystem.UpdateChemicals(uid, component.Solution, true);
                }

                UpdateUserInterface(uid, component);
            }
        }

        private void UpdateUserInterface(EntityUid uid, DeepFryerComponent component)
        {
            var state = new DeepFryerBoundUserInterfaceState(
                GetOilLevel(uid, component),
                GetOilPurity(uid, component),
                component.FryingOilThreshold,
                EntityManager.GetNetEntityArray(component.Storage.ContainedEntities.ToArray()));

            if (!_uiSystem.TrySetUiState(uid, DeepFryerUiKey.Key, state))
                _sawmill.Warning($"{ToPrettyString(uid)} was unable to set UI state.");
        }

        /// <summary>
        /// Does the deep fryer have hot oil?
        /// </summary>
        /// <remarks>
        /// This is mainly for audio.
        /// </remarks>
        private bool HasBubblingOil(EntityUid uid, DeepFryerComponent component)
        {
            return _powerReceiverSystem.IsPowered(uid) && GetOilVolume(uid, component) > FixedPoint2.Zero;
        }

        private void UpdateAmbientSound(EntityUid uid, DeepFryerComponent component)
        {
            _ambientSoundSystem.SetAmbience(uid, HasBubblingOil(uid, component));
        }

        private void UpdateNextFryTime(EntityUid uid, DeepFryerComponent component)
        {
            component.NextFryTime = _gameTimingSystem.CurTime + component.FryInterval;
        }

        /// <summary>
        /// Make an item look deep-fried.
        /// </summary>
        public void MakeCrispy(EntityUid item)
        {
            EnsureComp<AppearanceComponent>(item);
            EnsureComp<DeepFriedComponent>(item);

            _appearanceSystem.SetData(item, DeepFriedVisuals.Fried, true);
        }

        /// <summary>
        /// Turn a dead mob into food.
        /// </summary>
        /// <remarks>
        /// This is meant to be an irreversible process, similar to gibbing.
        /// </remarks>
        public bool TryMakeMobIntoFood(EntityUid mob, MobStateComponent mobStateComponent, bool force = false)
        {
            // Don't do anything to mobs until they're dead.
            if (force || _mobStateSystem.IsDead(mob, mobStateComponent))
            {
                RemComp<ActiveNPCComponent>(mob);
                RemComp<AtmosExposedComponent>(mob);
                RemComp<BarotraumaComponent>(mob);
                RemComp<BuckleComponent>(mob);
                RemComp<GhostTakeoverAvailableComponent>(mob);
                RemComp<InternalsComponent>(mob);
                RemComp<PerishableComponent>(mob);
                RemComp<RespiratorComponent>(mob);
                RemComp<RottingComponent>(mob);

                // Ensure it's Food here, so it passes the whitelist.
                var mobFoodComponent = EnsureComp<FoodComponent>(mob);
                var mobFoodSolution = _solutionContainerSystem.EnsureSolution(mob, mobFoodComponent.Solution, out bool alreadyHadFood);

                // This line here is mainly for mice, because they have a food
                // component that mirrors how much blood they have, which is
                // used for the reagent grinder.
                if (alreadyHadFood)
                    _solutionContainerSystem.RemoveAllSolution(mob, mobFoodSolution);

                if (TryComp<BloodstreamComponent>(mob, out var bloodstreamComponent))
                {
                    // Fry off any blood into protein.
                    var bloodSolution = bloodstreamComponent.BloodSolution;
                    var removedBloodQuantity = bloodSolution.RemoveReagent("Blood", FixedPoint2.MaxValue);
                    var proteinQuantity = removedBloodQuantity * BloodToProteinRatio;
                    mobFoodSolution.MaxVolume += proteinQuantity;
                    mobFoodSolution.AddReagent("Protein", proteinQuantity);

                    // This is a heuristic. If you had blood, you might just taste meaty.
                    if (removedBloodQuantity > FixedPoint2.Zero)
                        EnsureComp<FlavorProfileComponent>(mob).Flavors.Add(MobFlavorMeat);

                    // Bring in whatever chemicals they had in them too.
                    mobFoodSolution.MaxVolume += bloodstreamComponent.ChemicalSolution.Volume;
                    mobFoodSolution.AddSolution(bloodstreamComponent.ChemicalSolution, _prototypeManager);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Make an item actually edible.
        /// </summary>
        private void MakeEdible(EntityUid uid, DeepFryerComponent component, EntityUid item, FixedPoint2 solutionQuantity)
        {
            if (!TryComp<DeepFriedComponent>(item, out var deepFriedComponent))
            {
                _sawmill.Error($"{ToPrettyString(item)} is missing the DeepFriedComponent before being made Edible.");
                return;
            }

            // Remove any components that wouldn't make sense anymore.
            RemComp<ButcherableComponent>(item);

            if (TryComp<PaperComponent>(item, out var paperComponent))
            {
                var stringBuilder = new StringBuilder();

                for (var i = 0; i < paperComponent.Content.Length; ++i)
                {
                    var uchar = paperComponent.Content.Substring(i, 1);

                    if (uchar == "\n" || _random.Prob(0.4f))
                        stringBuilder.Append(uchar);
                    else
                        stringBuilder.Append("x");
                }

                paperComponent.Content = stringBuilder.ToString();
            }

            var foodComponent = EnsureComp<FoodComponent>(item);
            var extraSolution = new Solution();
            if (TryComp(item, out FlavorProfileComponent? flavorProfileComponent))
            {
                HashSet<string> goodFlavors = new(flavorProfileComponent.Flavors);
                goodFlavors.IntersectWith(component.GoodFlavors);

                HashSet<string> badFlavors = new(flavorProfileComponent.Flavors);
                badFlavors.IntersectWith(component.BadFlavors);

                deepFriedComponent.PriceCoefficient = Math.Max(0.01f,
                    1.0f
                    + goodFlavors.Count * component.GoodFlavorPriceBonus
                    - badFlavors.Count * component.BadFlavorPriceMalus);

                if (goodFlavors.Count > 0)
                    foreach (var reagent in component.GoodReagents)
                    {
                        extraSolution.AddReagent(reagent.Reagent.ToString(), reagent.Quantity * goodFlavors.Count);

                        // Mask the taste of "medicine."
                        flavorProfileComponent.IgnoreReagents.Add(reagent.Reagent.ToString());
                    }

                if (badFlavors.Count > 0)
                    foreach (var reagent in component.BadReagents)
                        extraSolution.AddReagent(reagent.Reagent.ToString(), reagent.Quantity * badFlavors.Count);
            }
            else
            {
                flavorProfileComponent = EnsureComp<FlavorProfileComponent>(item);
                // TODO: Default flavor?
            }

            // Make sure there's enough room for the fryer solution.
            var foodContainer = _solutionContainerSystem.EnsureSolution(item, foodComponent.Solution);

            // The solution quantity is used to give the fried food an extra
            // buffer too, to support injectables or condiments.
            foodContainer.MaxVolume = 2 * solutionQuantity + foodContainer.Volume + extraSolution.Volume;
            foodContainer.AddSolution(component.Solution.SplitSolution(solutionQuantity), _prototypeManager);
            foodContainer.AddSolution(extraSolution, _prototypeManager);
            _solutionContainerSystem.UpdateChemicals(item, foodContainer, true);
        }

        /// <summary>
        /// Returns how much total oil is in the vat.
        /// </summary>
        public FixedPoint2 GetOilVolume(EntityUid uid, DeepFryerComponent component)
        {
            var oilVolume = FixedPoint2.Zero;

            foreach (var reagent in component.Solution)
                if (component.FryingOils.Contains(reagent.Reagent.ToString()))
                    oilVolume += reagent.Quantity;

            return oilVolume;
        }

        /// <summary>
        /// Returns how much total waste is in the vat.
        /// </summary>
        public FixedPoint2 GetWasteVolume(EntityUid uid, DeepFryerComponent component)
        {
            var wasteVolume = FixedPoint2.Zero;

            foreach (var reagent in component.WasteReagents)
            {
                wasteVolume += component.Solution.GetReagentQuantity(reagent.Reagent);
            }

            return wasteVolume;
        }

        /// <summary>
        /// Returns a percentage of how much of the total solution is usable oil.
        /// </summary>
        public FixedPoint2 GetOilPurity(EntityUid uid, DeepFryerComponent component)
        {
            return GetOilVolume(uid, component) / component.Solution.Volume;
        }

        /// <summary>
        /// Returns a percentage of how much of the total volume is usable oil.
        /// </summary>
        public FixedPoint2 GetOilLevel(EntityUid uid, DeepFryerComponent component)
        {
            return GetOilVolume(uid, component) / component.Solution.MaxVolume;
        }

        /// <summary>
        /// This takes care of anything that would happen to an item with or
        /// without enough oil.
        /// </summary>
        private void CookItem(EntityUid uid, DeepFryerComponent component, EntityUid item)
        {
            if (TryComp<TemperatureComponent>(item, out var tempComp))
            {
                // Push the temperature towards what it should be but no higher.
                var delta = (component.PoweredTemperature - tempComp.CurrentTemperature) * tempComp.HeatCapacity;

                if (delta > 0f)
                    _temperature.ChangeHeat(item, delta, false, tempComp);
            }

            if (TryComp<SolutionContainerManagerComponent>(item, out var solutions))
            {
                foreach (var (_, solution) in solutions.Solutions)
                {
                    _solutionContainerSystem.SetTemperature(item, solution, component.PoweredTemperature);
                }
            }

            // Damage non-food items and mobs.
            if ((!HasComp<FoodComponent>(item) || HasComp<MobStateComponent>(item)) &&
                TryComp<DamageableComponent>(item, out var damageableComponent))
            {
                var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(CookingDamageType), CookingDamageAmount);

                var result = _damageableSystem.TryChangeDamage(item, damage, origin: uid);
                if (result?.Total > FixedPoint2.Zero)
                {
                    // TODO: Smoke, waste, sound, or some indication.
                }
            }
        }

        /// <summary>
        /// Destroy a food item and replace it with a charred mess.
        /// </summary>
        private void BurnItem(EntityUid uid, DeepFryerComponent component, EntityUid item)
        {
            if (HasComp<FoodComponent>(item) &&
                !HasComp<MobStateComponent>(item) &&
                MetaData(item).EntityPrototype?.ID != component.CharredPrototype)
            {
                var charred = Spawn(component.CharredPrototype, Transform(uid).Coordinates);
                component.Storage.Insert(charred);
                Del(item);
            }
        }

        private void UpdateDeepFriedName(EntityUid uid, DeepFriedComponent component)
        {
            if (component.OriginalName == null)
                return;

            switch (component.Crispiness)
            {
                case 0:
                    // Already handled at OnInitDeepFried.
                    break;
                case 1:
                    _metaDataSystem.SetEntityName(uid, Loc.GetString("deep-fried-crispy-item",
                        ("entity", component.OriginalName)));
                    break;
                default:
                    _metaDataSystem.SetEntityName(uid, Loc.GetString("deep-fried-burned-item",
                        ("entity", component.OriginalName)));
                    break;
            }
        }

        /// <summary>
        /// Try to deep fry a single item, which can
        ///  - be cancelled by other systems, or
        ///  - fail due to the blacklist, or
        ///  - give it a crispy shader, and possibly also
        ///  - turn it into food.
        /// </summary>
        private void DeepFry(EntityUid uid, DeepFryerComponent component, EntityUid item)
        {
            if (MetaData(item).EntityPrototype?.ID == component.CharredPrototype)
                return;

            // This item has already been deep-fried, and now it's progressing
            // into another stage.
            if (TryComp<DeepFriedComponent>(item, out var deepFriedComponent))
            {
                // TODO: Smoke, waste, sound, or some indication.

                deepFriedComponent.Crispiness += 1;

                if (deepFriedComponent.Crispiness > MaximumCrispiness)
                {
                    BurnItem(uid, component, item);
                    return;
                }

                UpdateDeepFriedName(item, deepFriedComponent);
                return;
            }

            // Allow entity systems to conditionally forbid an attempt at deep-frying.
            var attemptEvent = new DeepFryAttemptEvent(uid);
            RaiseLocalEvent(item, attemptEvent);

            if (attemptEvent.Cancelled)
                return;

            // The attempt event is allowed to go first before the blacklist check,
            // just in case the attempt is relevant to any system in the future.
            //
            // The blacklist overrides all.
            if (component.Blacklist != null && component.Blacklist.IsValid(item, EntityManager))
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-blacklist-item-failed",
                        ("item", item), ("deepFryer", uid)),
                    uid,
                    Filter.Pvs(uid, PvsWarningRange),
                    true);
                return;
            }

            var beingEvent = new BeingDeepFriedEvent(uid, item);
            RaiseLocalEvent(item, beingEvent);

            // It's important to check for the MobStateComponent so we know
            // it's actually a mob, because functions like
            // MobStateSystem.IsAlive will return false if the entity lacks the
            // component.
            if (TryComp<MobStateComponent>(item, out var mobStateComponent))
                if (!TryMakeMobIntoFood(item, mobStateComponent))
                    return;

            MakeCrispy(item);

            var itemComponent = Comp<ItemComponent>(item);

            // Determine how much solution to spend on this item.
            var solutionQuantity = FixedPoint2.Min(
                component.Solution.Volume,
                itemComponent.Size * component.SolutionSizeCoefficient);

            if (component.Whitelist != null && component.Whitelist.IsValid(item, EntityManager) ||
                beingEvent.TurnIntoFood)
            {
                MakeEdible(uid, component, item, solutionQuantity);
            }
            else
            {
                component.Solution.RemoveSolution(solutionQuantity);
            }

            component.WasteToAdd += solutionQuantity;
        }

        private void OnInitDeepFryer(EntityUid uid, DeepFryerComponent component, ComponentInit args)
        {
            component.Storage = _containerSystem.EnsureContainer<Container>(uid, component.StorageName, out bool containerExisted);

            if (!containerExisted)
                _sawmill.Warning($"{ToPrettyString(uid)} did not have a {component.StorageName} container. It has been created.");

            component.Solution = _solutionContainerSystem.EnsureSolution(uid, component.SolutionName, out bool solutionExisted);

            if (!solutionExisted)
                _sawmill.Warning($"{ToPrettyString(uid)} did not have a {component.SolutionName} solution container. It has been created.");
            foreach (var reagent in component.Solution.Contents.ToArray())
            {
                //JJ Comment - not sure this works. Need to check if Reagent.ToString is correct.
                _prototypeManager.TryIndex<ReagentPrototype>(reagent.Reagent.ToString(), out var proto);
                var effectsArgs = new ReagentEffectArgs(uid,
                        null,
                        component.Solution,
                        proto!,
                        reagent.Quantity,
                        EntityManager,
                        null,
                        1f);
                foreach (var effect in component.UnsafeOilVolumeEffects)
                {
                    if (!effect.ShouldApply(effectsArgs, _random))
                        continue;
                    effect.Effect(effectsArgs);
                }
            }
        }

        /// <summary>
        /// Make sure the UI and interval tracker are updated anytime something
        /// is inserted into one of the baskets.
        /// </summary>
        /// <remarks>
        /// This is used instead of EntInsertedIntoContainerMessage so charred
        /// items can be inserted into the deep fryer without triggering this
        /// event.
        /// </remarks>
        private void AfterInsert(EntityUid uid, DeepFryerComponent component, EntityUid item)
        {
            if (HasBubblingOil(uid, component))
                _audioSystem.PlayPvs(component.SoundInsertItem, uid, AudioParamsInsertRemove);

            UpdateNextFryTime(uid, component);
            UpdateUserInterface(uid, component);
        }

        private void OnPowerChange(EntityUid uid, DeepFryerComponent component, ref PowerChangedEvent args)
        {
            _appearanceSystem.SetData(uid, DeepFryerVisuals.Bubbling, args.Powered);
            UpdateNextFryTime(uid, component);
            UpdateAmbientSound(uid, component);
        }

        private void OnDeconstruct(EntityUid uid, DeepFryerComponent component, MachineDeconstructedEvent args)
        {
            // The EmptyOnMachineDeconstruct component handles the entity container for us.
            _puddleSystem.TrySpillAt(uid, component.Solution, out var _);
        }

        private void OnDestruction(EntityUid uid, DeepFryerComponent component, DestructionEventArgs args)
        {
            _containerSystem.EmptyContainer(component.Storage, true);
        }

        private void OnRefreshParts(EntityUid uid, DeepFryerComponent component, RefreshPartsEvent args)
        {
            var ratingStorage = args.PartRatings[component.MachinePartStorageMax];

            component.StorageMaxEntities = component.BaseStorageMaxEntities + (int) (component.StoragePerPartRating * (ratingStorage - 1));
        }

        /// <summary>
        /// Allow thrown items to land in a basket.
        /// </summary>
        private void OnThrowHitBy(EntityUid uid, DeepFryerComponent component, ThrowHitByEvent args)
        {
            if (args.Handled)
                return;

            // Chefs never miss this. :)
            float missChance = HasComp<ProfessionalChefComponent>(args.User) ? 0f : ThrowMissChance;

            if (!CanInsertItem(uid, component, args.Thrown) ||
                _random.Prob(missChance) ||
                !component.Storage.Insert(args.Thrown))
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-thrown-missed"),
                    uid);

                if (args.User != null)
                    _adminLogManager.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(args.User.Value)} threw {ToPrettyString(args.Thrown)} at {ToPrettyString(uid)}, and it missed.");

                return;
            }

            if (GetOilVolume(uid, component) < component.SafeOilVolume)
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-thrown-hit-oil-low"),
                    uid);
            else
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-thrown-hit-oil"),
                    uid);

            if (args.User != null)
                _adminLogManager.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(args.User.Value)} threw {ToPrettyString(args.Thrown)} at {ToPrettyString(uid)}, and it landed inside.");

            AfterInsert(uid, component, args.Thrown);

            args.Handled = true;
        }

        private void OnSolutionChange(EntityUid uid, DeepFryerComponent component, SolutionChangedEvent args)
        {
            UpdateUserInterface(uid, component);
            UpdateAmbientSound(uid, component);
        }

        private void OnRelayMovement(EntityUid uid, DeepFryerComponent component, ref ContainerRelayMovementEntityEvent args)
        {
            if (!component.Storage.Remove(args.Entity, EntityManager, destination: Transform(uid).Coordinates))
                return;

            _popupSystem.PopupEntity(
                Loc.GetString("deep-fryer-entity-escape",
                    ("victim", Identity.Entity(args.Entity, EntityManager)),
                    ("deepFryer", uid)),
                uid,
                PopupType.SmallCaution);
        }

        public bool CanInsertItem(EntityUid uid, DeepFryerComponent component, EntityUid item)
        {
            // Keep this consistent with the checks in TryInsertItem.
            return (HasComp<ItemComponent>(item) &&
                !HasComp<StorageComponent>(item) &&
                component.Storage.ContainedEntities.Count < component.StorageMaxEntities);
        }

        private bool TryInsertItem(EntityUid uid, DeepFryerComponent component, EntityUid user, EntityUid item)
        {
            if (!HasComp<ItemComponent>(item))
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-interact-using-not-item"),
                    uid,
                    user);
                return false;
            }

            if (HasComp<StorageComponent>(item))
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-storage-no-fit",
                        ("item", item)),
                    uid,
                    user);
                return false;
            }

            if (component.Storage.ContainedEntities.Count >= component.StorageMaxEntities)
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-storage-full"),
                    uid,
                    user);
                return false;
            }

            if (!_handsSystem.TryDropIntoContainer(user, item, component.Storage))
                return false;

            AfterInsert(uid, component, item);

            _adminLogManager.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user)} put {ToPrettyString(item)} inside {ToPrettyString(uid)}.");

            return true;
        }

        private void OnInteractUsing(EntityUid uid, DeepFryerComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // By default, allow entities with SolutionTransfer or Tool
            // components to perform their usual actions. Inserting them (if
            // the chef really wants to) will be supported through the UI.
            if (HasComp<SolutionTransferComponent>(args.Used) ||
                HasComp<ToolComponent>(args.Used))
            {
                return;
            }

            if (TryInsertItem(uid, component, args.User, args.Used))
                args.Handled = true;
        }

        private void OnBeforeActivatableUIOpen(EntityUid uid, DeepFryerComponent component, BeforeActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnRemoveItem(EntityUid uid, DeepFryerComponent component, DeepFryerRemoveItemMessage args)
        {
            var removedItem = EntityManager.GetEntity(args.Item);
            if (removedItem.Valid)
            { //JJ Comment - This line should be unnecessary. Some issue is keeping the UI from updating when converting straight to a Burned Mess while the UI is still open. To replicate, put a Raw Meat in the fryer with no oil in it. Wait until it sputters with no effect. It should transform to Burned Mess, but doesn't.
                if (!component.Storage.Remove(removedItem))
                    return;

                var user = args.Session.AttachedEntity;

                if (user != null)
                {
                    _handsSystem.TryPickupAnyHand(user.Value, removedItem);

                    _adminLogManager.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(user.Value)} took {ToPrettyString(args.Item)} out of {ToPrettyString(uid)}.");
                }

                _audioSystem.PlayPvs(component.SoundRemoveItem, uid, AudioParamsInsertRemove);

                UpdateUserInterface(component.Owner, component);
            }
        }

        private void OnInsertItem(EntityUid uid, DeepFryerComponent component, DeepFryerInsertItemMessage args)
        {
            var user = args.Session.AttachedEntity;

            if (user == null ||
                !TryComp<HandsComponent>(user, out var handsComponent) ||
                handsComponent.ActiveHandEntity == null)
            {
                return;
            }

            if (handsComponent.ActiveHandEntity != null)
                TryInsertItem(uid, component, user.Value, handsComponent.ActiveHandEntity.Value);
        }

        /// <summary>
        /// This is a helper function for ScoopVat and ClearSlag.
        /// </summary>
        private bool TryGetActiveHandSolutionContainer(
            EntityUid fryer,
            EntityUid user,
            [NotNullWhen(true)] out EntityUid? heldItem,
            [NotNullWhen(true)] out Solution? solution,
            out FixedPoint2 transferAmount)
        {
            heldItem = null;
            solution = null;
            transferAmount = FixedPoint2.Zero;

            if (!TryComp<HandsComponent>(user, out var handsComponent))
                return false;

            heldItem = handsComponent.ActiveHandEntity;

            if (heldItem == null ||
                !TryComp<SolutionTransferComponent>(heldItem, out var solutionTransferComponent) ||
                !_solutionContainerSystem.TryGetRefillableSolution(heldItem.Value, out var refillableSolution) ||
                !solutionTransferComponent.CanReceive)
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-need-liquid-container-in-hand"),
                    fryer,
                    user);

                return false;
            }

            solution = refillableSolution;
            transferAmount = solutionTransferComponent.TransferAmount;

            return true;
        }

        private void OnScoopVat(EntityUid uid, DeepFryerComponent component, DeepFryerScoopVatMessage args)
        {
            var user = args.Session.AttachedEntity;

            if (user == null ||
                !TryGetActiveHandSolutionContainer(uid, user.Value, out var heldItem, out var heldSolution, out var transferAmount))
            {
                return;
            }

            _solutionTransferSystem.Transfer(user.Value,
                uid,
                component.Solution,
                heldItem.Value,
                heldSolution,
                transferAmount);

            // UI update is not necessary here, because the solution change event handles it.
        }

        private void OnClearSlagStart(EntityUid uid, DeepFryerComponent component, DeepFryerClearSlagMessage args)
        {
            var user = args.Session.AttachedEntity;

            if (user == null ||
                !TryGetActiveHandSolutionContainer(uid, user.Value, out var heldItem, out var heldSolution, out var transferAmount))
            {
                return;
            }

            var wasteVolume = GetWasteVolume(uid, component);
            if (wasteVolume == FixedPoint2.Zero)
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("deep-fryer-oil-no-slag"),
                    uid,
                    user.Value);

                return;
            }

            var delay = Math.Clamp((float) wasteVolume * 0.1f, 1f, 5f);

            var ev = new ClearSlagDoAfterEvent(heldSolution, transferAmount);

            //JJ Comment - not sure I have DoAfterArgs configured correctly.
            var doAfterArgs = new DoAfterArgs(EntityManager, user.Value, delay, ev, uid, uid, used: heldItem)
            {
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.25f,
                NeedHand = true,
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
        }

        private void OnRemoveAllItems(EntityUid uid, DeepFryerComponent component, DeepFryerRemoveAllItemsMessage args)
        {
            if (component.Storage.ContainedEntities.Count == 0)
                return;

            _containerSystem.EmptyContainer(component.Storage, false);

            var user = args.Session.AttachedEntity;

            if (user != null)
                _adminLogManager.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user.Value)} removed all items from {ToPrettyString(uid)}.");

            _audioSystem.PlayPvs(component.SoundRemoveItem, uid, AudioParamsInsertRemove);

            UpdateUserInterface(component.Owner, component);
        }

        private void OnClearSlag(EntityUid uid, DeepFryerComponent component, ClearSlagDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Used == null)
                return;

            FixedPoint2 reagentCount = component.WasteReagents.Count();

            var removingSolution = new Solution();
            foreach (var reagent in component.WasteReagents)
            {
                var removed = component.Solution.RemoveReagent(reagent.Reagent.ToString(), args.Amount / reagentCount);
                removingSolution.AddReagent(reagent.Reagent.ToString(), removed);
            }

            _solutionContainerSystem.UpdateChemicals(uid, component.Solution);
            _solutionContainerSystem.TryMixAndOverflow(args.Args.Used.Value, args.Solution, removingSolution, args.Solution.MaxVolume, out var _);
        }
        private void OnInitDeepFried(EntityUid uid, DeepFriedComponent component, ComponentInit args)
        {
            var meta = MetaData(uid);
            component.OriginalName = meta.EntityName;
            _metaDataSystem.SetEntityName(uid, Loc.GetString("deep-fried-crispy-item", ("entity", meta.EntityName)));
        }

        private void OnExamineFried(EntityUid uid, DeepFriedComponent component, ExaminedEvent args)
        {
            switch (component.Crispiness)
            {
                case 0:
                    args.PushMarkup(Loc.GetString("deep-fried-crispy-item-examine"));
                    break;
                case 1:
                    args.PushMarkup(Loc.GetString("deep-fried-fried-item-examine"));
                    break;
                default:
                    args.PushMarkup(Loc.GetString("deep-fried-burned-item-examine"));
                    break;
            }
        }

        private void OnPriceCalculation(EntityUid uid, DeepFriedComponent component, ref PriceCalculationEvent args)
        {
            args.Price *= component.PriceCoefficient;
        }

        private void OnSliceDeepFried(EntityUid uid, DeepFriedComponent component, SliceFoodEvent args)
        {
            MakeCrispy(args.Slice);

            // Copy relevant values to the slice.
            var sourceDeepFriedComponent = Comp<DeepFriedComponent>(args.Food);
            var sliceDeepFriedComponent = Comp<DeepFriedComponent>(args.Slice);

            sliceDeepFriedComponent.Crispiness = sourceDeepFriedComponent.Crispiness;
            sliceDeepFriedComponent.PriceCoefficient = sourceDeepFriedComponent.PriceCoefficient;

            UpdateDeepFriedName(args.Slice, sliceDeepFriedComponent);

            // TODO: Flavor profiles aren't copied to the slices. This should
            // probably be handled on upstream, but for now let's assume the
            // oil of the deep fryer is overpowering enough for this small
            // hack. This is likely the only place where it would be useful.
            if (TryComp<FlavorProfileComponent>(args.Food, out var sourceFlavorProfileComponent) &&
                TryComp<FlavorProfileComponent>(args.Slice, out var sliceFlavorProfileComponent))
            {
                sliceFlavorProfileComponent.Flavors.UnionWith(sourceFlavorProfileComponent.Flavors);
                sliceFlavorProfileComponent.IgnoreReagents.UnionWith(sourceFlavorProfileComponent.IgnoreReagents);
            }
        }
    }


    public sealed class DeepFryAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid DeepFryer { get; }

        public DeepFryAttemptEvent(EntityUid deepFryer)
        {
            DeepFryer = deepFryer;
        }
    }

    public sealed class BeingDeepFriedEvent : EntityEventArgs
    {
        public EntityUid DeepFryer { get; }
        public EntityUid Item { get; }
        public bool TurnIntoFood { get; set; }

        public BeingDeepFriedEvent(EntityUid deepFryer, EntityUid item)
        {
            DeepFryer = deepFryer;
            Item = item;
        }
    }
}

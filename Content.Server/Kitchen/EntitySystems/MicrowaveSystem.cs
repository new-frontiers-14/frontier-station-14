using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Kitchen.EntitySystems
{
    public sealed class MicrowaveSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly RecipeManager _recipeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly TemperatureSystem _temperature = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<MicrowaveComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
            SubscribeLocalEvent<MicrowaveComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<MicrowaveComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<MicrowaveComponent, SuicideEvent>(OnSuicide);
            SubscribeLocalEvent<MicrowaveComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<MicrowaveComponent, UpgradeExamineEvent>(OnUpgradeExamine);

            SubscribeLocalEvent<MicrowaveComponent, MicrowaveStartCookMessage>((u, c, m) => Wzhzhzh(u, c, m.Session.AttachedEntity));
            SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectMessage>(OnEjectMessage);
            SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectSolidIndexedMessage>(OnEjectIndex);
            SubscribeLocalEvent<MicrowaveComponent, MicrowaveSelectCookTimeMessage>(OnSelectTime);

            SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentStartup>(OnCookStart);
            SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentShutdown>(OnCookStop);
        }

        private void OnCookStart(EntityUid uid, ActiveMicrowaveComponent component, ComponentStartup args)
        {
            if (!TryComp<MicrowaveComponent>(uid, out var microwaveComponent))
                return;
            SetAppearance(uid, MicrowaveVisualState.Cooking, microwaveComponent);

            microwaveComponent.PlayingStream =
                _audio.PlayPvs(microwaveComponent.LoopingSound, uid, AudioParams.Default.WithLoop(true).WithMaxDistance(5));
        }

        private void OnCookStop(EntityUid uid, ActiveMicrowaveComponent component, ComponentShutdown args)
        {
            if (!TryComp<MicrowaveComponent>(uid, out var microwaveComponent))
                return;
            SetAppearance(uid, MicrowaveVisualState.Idle, microwaveComponent);

            microwaveComponent.PlayingStream?.Stop();
        }

        /// <summary>
        ///     Adds temperature to every item in the microwave,
        ///     based on the time it took to microwave.
        /// </summary>
        /// <param name="component">The microwave that is heating up.</param>
        /// <param name="time">The time on the microwave, in seconds.</param>
        private void AddTemperature(MicrowaveComponent component, float time)
        {
            var heatToAdd = time * 100;
            foreach (var entity in component.Storage.ContainedEntities)
            {
                if (TryComp<TemperatureComponent>(entity, out var tempComp))
                    _temperature.ChangeHeat(entity, heatToAdd, false, tempComp);

                if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                    continue;
                foreach (var (_, solution) in solutions.Solutions)
                {
                    if (solution.Temperature > component.TemperatureUpperThreshold)
                        continue;

                    _solutionContainer.AddThermalEnergy(entity, solution, heatToAdd);
                }
            }
        }

        private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe)
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]

            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

            // this is spaghetti ngl
            foreach (var item in component.Storage.ContainedEntities)
            {
                if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                    continue;

                // go over every solution
                foreach (var (_, solution) in solMan.Solutions)
                {
                    foreach (var (reagent, _) in recipe.IngredientsReagents)
                    {
                        // removed everything
                        if (!totalReagentsToRemove.ContainsKey(reagent))
                            continue;

                        var quant = solution.GetTotalPrototypeQuantity(reagent);

                        if (quant >= totalReagentsToRemove[reagent])
                        {
                            quant = totalReagentsToRemove[reagent];
                            totalReagentsToRemove.Remove(reagent);
                        }
                        else
                        {
                            totalReagentsToRemove[reagent] -= quant;
                        }

                        _solutionContainer.RemoveReagent(item, solution, reagent, quant);
                    }
                }
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in component.Storage.ContainedEntities)
                    {
                        var metaData = MetaData(item);
                        if (metaData.EntityPrototype == null)
                        {
                            continue;
                        }

                        if (metaData.EntityPrototype.ID == recipeSolid.Key)
                        {
                            component.Storage.Remove(item);
                            EntityManager.DeleteEntity(item);
                            break;
                        }
                    }
                }
            }
        }

        private void OnInit(EntityUid uid, MicrowaveComponent component, ComponentInit ags)
        {
            component.Storage = _container.EnsureContainer<Container>(uid, "microwave_entity_container");
        }

        private void OnSuicide(EntityUid uid, MicrowaveComponent component, SuicideEvent args)
        {
            if (args.Handled)
                return;

            args.SetHandled(SuicideKind.Heat);
            var victim = args.Victim;
            var headCount = 0;

            if (TryComp<BodyComponent>(victim, out var body))
            {
                var headSlots = _bodySystem.GetBodyChildrenOfType(victim, BodyPartType.Head, body);

                foreach (var part in headSlots)
                {
                    if (!_bodySystem.OrphanPart(part.Id, part.Component))
                    {
                        continue;
                    }

                    component.Storage.Insert(part.Id);
                    headCount++;
                }
            }

            var othersMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-others-message", ("victim", victim))
                : Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));

            var selfMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-message")
                : Loc.GetString("microwave-component-suicide-message");

            _popupSystem.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
            _popupSystem.PopupEntity(selfMessage, victim, victim);

            _audio.PlayPvs(component.ClickSound, uid, AudioParams.Default.WithVolume(-2));
            component.CurrentCookTimerTime = 10;
            Wzhzhzh(uid, component, args.Victim);
            UpdateUserInterfaceState(uid, component);
        }

        private void OnSolutionChange(EntityUid uid, MicrowaveComponent component, SolutionChangedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        private void OnInteractUsing(EntityUid uid, MicrowaveComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;
            if (!(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-no-power"), uid, args.User);
                return;
            }

            if (component.Broken)
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-broken"), uid, args.User);
                return;
            }

            if (!HasComp<ItemComponent>(args.Used))
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), uid, args.User);
                return;
            }

            args.Handled = true;
            _handsSystem.TryDropIntoContainer(args.User, args.Used, component.Storage);
            UpdateUserInterfaceState(uid, component);
        }

        private void OnBreak(EntityUid uid, MicrowaveComponent component, BreakageEventArgs args)
        {
            component.Broken = true;
            SetAppearance(uid, MicrowaveVisualState.Broken, component);
            RemComp<ActiveMicrowaveComponent>(uid);
            _sharedContainer.EmptyContainer(component.Storage);
            UpdateUserInterfaceState(uid, component);
        }

        private void OnPowerChanged(EntityUid uid, MicrowaveComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                SetAppearance(uid, MicrowaveVisualState.Idle, component);
                RemComp<ActiveMicrowaveComponent>(uid);
                _sharedContainer.EmptyContainer(component.Storage);
            }
            UpdateUserInterfaceState(uid, component);
        }

        private void OnRefreshParts(EntityUid uid, MicrowaveComponent component, RefreshPartsEvent args)
        {
            var cookRating = args.PartRatings[component.MachinePartCookTimeMultiplier];
            component.CookTimeMultiplier = MathF.Pow(component.CookTimeScalingConstant, cookRating - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, MicrowaveComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("microwave-component-upgrade-cook-time", component.CookTimeMultiplier);
        }

        public void UpdateUserInterfaceState(EntityUid uid, MicrowaveComponent component)
        {
            var ui = _userInterface.GetUiOrNull(uid, MicrowaveUiKey.Key);
            if (ui == null)
                return;

            _userInterface.SetUiState(ui, new MicrowaveUpdateUserInterfaceState(
                GetNetEntityArray(component.Storage.ContainedEntities.ToArray()),
                HasComp<ActiveMicrowaveComponent>(uid),
                component.CurrentCookTimeButtonIndex,
                component.CurrentCookTimerTime
            ));
        }

        public void SetAppearance(EntityUid uid, MicrowaveVisualState state, MicrowaveComponent? component = null, AppearanceComponent? appearanceComponent = null)
        {
            if (!Resolve(uid, ref component, ref appearanceComponent, false))
                return;
            var display = component.Broken ? MicrowaveVisualState.Broken : state;
            _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
        }

        public static bool HasContents(MicrowaveComponent component)
        {
            return component.Storage.ContainedEntities.Any();
        }

        /// <summary>
        /// Starts Cooking
        /// </summary>
        /// <remarks>
        /// It does not make a "wzhzhzh" sound, it makes a "mmmmmmmm" sound!
        /// -emo
        /// </remarks>
        public void Wzhzhzh(EntityUid uid, MicrowaveComponent component, EntityUid? user)
        {
            if (!HasContents(component) || HasComp<ActiveMicrowaveComponent>(uid))
                return;

            var solidsDict = new Dictionary<string, int>();
            var reagentDict = new Dictionary<string, FixedPoint2>();
            // TODO use lists of Reagent quantities instead of reagent prototype ids.

            foreach (var item in component.Storage.ContainedEntities)
            {
                // special behavior when being microwaved ;)
                var ev = new BeingMicrowavedEvent(uid, user);
                RaiseLocalEvent(item, ev);

                if (ev.Handled)
                {
                    UpdateUserInterfaceState(uid, component);
                    return;
                }

                // destroy microwave
                if (_tag.HasTag(item, "MicrowaveMachineUnsafe") || _tag.HasTag(item, "Metal"))
                {
                    component.Broken = true;
                    SetAppearance(uid, MicrowaveVisualState.Broken, component);
                    _audio.PlayPvs(component.ItemBreakSound, uid);
                    return;
                }

                if (_tag.HasTag(item, "MicrowaveSelfUnsafe") || _tag.HasTag(item, "Plastic"))
                {
                    var junk = Spawn(component.BadRecipeEntityId, Transform(uid).Coordinates);
                    component.Storage.Insert(junk);
                    QueueDel(item);
                }

                var metaData = MetaData(item); //this simply begs for cooking refactor
                if (metaData.EntityPrototype == null)
                    continue;

                if (solidsDict.ContainsKey(metaData.EntityPrototype.ID))
                {
                    solidsDict[metaData.EntityPrototype.ID]++;
                }
                else
                {
                    solidsDict.Add(metaData.EntityPrototype.ID, 1);
                }

                if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                    continue;

                foreach (var (_, solution) in solMan.Solutions)
                {
                    foreach (var (reagent, quantity) in solution.Contents)
                    {
                        if (reagentDict.ContainsKey(reagent.Prototype))
                            reagentDict[reagent.Prototype] += quantity;
                        else
                            reagentDict.Add(reagent.Prototype, quantity);
                    }
                }
            }

            // Check recipes
            var portionedRecipe = _recipeManager.Recipes.Select(r =>
                CanSatisfyRecipe(component, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);

            _audio.PlayPvs(component.StartCookingSound, uid);
            var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
            activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
            activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
            activeComp.PortionedRecipe = portionedRecipe;
            UpdateUserInterfaceState(uid, component);
        }

        public static (FoodRecipePrototype, int) CanSatisfyRecipe(MicrowaveComponent component, FoodRecipePrototype recipe, Dictionary<string, int> solids, Dictionary<string, FixedPoint2> reagents)
        {
            var portions = 0;

            if (component.CurrentCookTimerTime % recipe.CookTime != 0)
            {
                //can't be a multiple of this recipe
                return (recipe, 0);
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                if (!solids.ContainsKey(solid.Key))
                    return (recipe, 0);

                if (solids[solid.Key] < solid.Value)
                    return (recipe, 0);

                portions = portions == 0
                    ? solids[solid.Key] / solid.Value.Int()
                    : Math.Min(portions, solids[solid.Key] / solid.Value.Int());
            }

            foreach (var reagent in recipe.IngredientsReagents)
            {
                // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]
                if (!reagents.ContainsKey(reagent.Key))
                    return (recipe, 0);

                if (reagents[reagent.Key] < reagent.Value)
                    return (recipe, 0);

                portions = portions == 0
                    ? reagents[reagent.Key].Int() / reagent.Value.Int()
                    : Math.Min(portions, reagents[reagent.Key].Int() / reagent.Value.Int());
            }

            //cook only as many of those portions as time allows
            return (recipe, (int) Math.Min(portions, component.CurrentCookTimerTime / recipe.CookTime));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveMicrowaveComponent, MicrowaveComponent>();
            while (query.MoveNext(out var uid, out var active, out var microwave))
            {
                //check if there's still cook time left
                active.CookTimeRemaining -= frameTime;
                if (active.CookTimeRemaining > 0)
                    continue;

                //this means the microwave has finished cooking.
                AddTemperature(microwave, active.TotalTime);

                if (active.PortionedRecipe.Item1 != null)
                {
                    var coords = Transform(uid).Coordinates;
                    for (var i = 0; i < active.PortionedRecipe.Item2; i++)
                    {
                        SubtractContents(microwave, active.PortionedRecipe.Item1);
                        Spawn(active.PortionedRecipe.Item1.Result, coords);
                    }
                }

                _sharedContainer.EmptyContainer(microwave.Storage);
                UpdateUserInterfaceState(uid, microwave);
                EntityManager.RemoveComponentDeferred<ActiveMicrowaveComponent>(uid);
                _audio.PlayPvs(microwave.FoodDoneSound, uid, AudioParams.Default.WithVolume(-1));
            }
        }

        #region ui
        private void OnEjectMessage(EntityUid uid, MicrowaveComponent component, MicrowaveEjectMessage args)
        {
            if (!HasContents(component) || HasComp<ActiveMicrowaveComponent>(uid))
                return;

            _sharedContainer.EmptyContainer(component.Storage);
            _audio.PlayPvs(component.ClickSound, uid, AudioParams.Default.WithVolume(-2));
            UpdateUserInterfaceState(uid, component);
        }

        private void OnEjectIndex(EntityUid uid, MicrowaveComponent component, MicrowaveEjectSolidIndexedMessage args)
        {
            if (!HasContents(component) || HasComp<ActiveMicrowaveComponent>(uid))
                return;

            component.Storage.Remove(EntityManager.GetEntity(args.EntityID));
            UpdateUserInterfaceState(uid, component);
        }

        private void OnSelectTime(EntityUid uid, MicrowaveComponent comp, MicrowaveSelectCookTimeMessage args)
        {
            if (!HasContents(comp) || HasComp<ActiveMicrowaveComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
                return;

            // some validation to prevent trollage
            if (args.NewCookTime % 5 != 0 || args.NewCookTime > comp.MaxCookTime)
                return;

            comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
            comp.CurrentCookTimerTime = args.NewCookTime;
            _audio.PlayPvs(comp.ClickSound, uid, AudioParams.Default.WithVolume(-2));
            UpdateUserInterfaceState(uid, comp);
        }
        #endregion
    }
}

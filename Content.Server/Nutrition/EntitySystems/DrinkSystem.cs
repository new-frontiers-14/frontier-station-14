using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.ReagentEffects;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Inventory;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class DrinkSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO add InteractNoHandEvent for entities like mice.
        SubscribeLocalEvent<DrinkComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
        // run before inventory so for bucket it always tries to drink before equipping (when empty)
        // run after openable so its always open -> drink
        SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUse, before: new[] { typeof(ServerInventorySystem) }, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<DrinkComponent, GetVerbsEvent<AlternativeVerb>>(AddDrinkVerb);
        // put drink amount after opened
        SubscribeLocalEvent<DrinkComponent, ExaminedEvent>(OnExamined, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<DrinkComponent, ConsumeDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<PressurizedDrinkComponent, LandEvent>(OnPressurizedDrinkLand);
    }

    private FixedPoint2 DrinkVolume(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var sol))
            return FixedPoint2.Zero;

        return sol.Volume;
    }

    public bool IsEmpty(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        return DrinkVolume(uid, component) <= 0;
    }

    /// <summary>
    /// Get the total hydration factor contained in a drink's solution.
    /// </summary>
    public float TotalHydration(EntityUid uid, DrinkComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return 0f;

        if (!_solutionContainer.TryGetSolution(uid, comp.Solution, out var solution))
            return 0f;

        var total = 0f;
        foreach (var quantity in solution.Contents)
        {
            var reagent = _proto.Index<ReagentPrototype>(quantity.Reagent.Prototype);
            if (reagent.Metabolisms == null)
                continue;

            foreach ((var _, var entry) in reagent.Metabolisms)
            {
                foreach (var effect in entry.Effects)
                {
                    // ignores any effect conditions, just cares about how much it can hydrate
                    if (effect is SatiateThirst thirst)
                    {
                        total += thirst.HydrationFactor * quantity.Quantity.Float();
                    }
                }
            }
        }

        return total;
    }

    private void OnExamined(EntityUid uid, DrinkComponent component, ExaminedEvent args)
    {
        var hasOpenable = TryComp<OpenableComponent>(uid, out var openable);
        if (_openable.IsClosed(uid, null, openable) || !args.IsInDetailsRange || !component.Examinable)
            return;

        // put Empty / Xu after Opened, or start a new line
        args.Message.AddMarkup(hasOpenable ? " - " : "\n");

        var empty = IsEmpty(uid, component);
        if (empty)
        {
            args.Message.AddMarkup(Loc.GetString("drink-component-on-examine-is-empty"));
            return;
        }

        if (TryComp<ExaminableSolutionComponent>(uid, out var comp))
        {
            //provide exact measurement for beakers
            args.Message.AddMarkup(Loc.GetString("drink-component-on-examine-exact-volume", ("amount", DrinkVolume(uid, component))));
        }
        else
        {
            //general approximation
            var remainingString = (int) _solutionContainer.PercentFull(uid) switch
            {
                100 => "drink-component-on-examine-is-full",
                > 66 => "drink-component-on-examine-is-mostly-full",
                > 33 => HalfEmptyOrHalfFull(args),
                _ => "drink-component-on-examine-is-mostly-empty",
            };
            args.Message.AddMarkup(Loc.GetString(remainingString));
        }
    }

    private void AfterInteract(EntityUid uid, DrinkComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryDrink(args.User, args.Target.Value, component, uid);
    }

    private void OnUse(EntityUid uid, DrinkComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryDrink(args.User, args.User, component, uid);
    }

    private void OnPressurizedDrinkLand(EntityUid uid, PressurizedDrinkComponent comp, ref LandEvent args)
    {
        if (!TryComp<DrinkComponent>(uid, out var drink) || !TryComp<OpenableComponent>(uid, out var openable))
            return;

        if (!openable.Opened &&
            _random.Prob(comp.BurstChance) &&
            _solutionContainer.TryGetSolution(uid, drink.Solution, out var interactions))
        {
            // using SetOpen instead of TryOpen to not play 2 sounds
            _openable.SetOpen(uid, true, openable);

            var solution = _solutionContainer.SplitSolution(uid, interactions, interactions.Volume);
            _puddle.TrySpillAt(uid, solution, out _);

            _audio.PlayPvs(comp.BurstSound, uid);
        }
    }

    private void OnDrinkInit(EntityUid uid, DrinkComponent component, ComponentInit args)
    {
        if (TryComp<DrainableSolutionComponent>(uid, out var existingDrainable))
        {
            // Beakers have Drink component but they should use the existing Drainable
            component.Solution = existingDrainable.Solution;
        }
        else
        {
            _solutionContainer.EnsureSolution(uid, component.Solution);
        }

        UpdateAppearance(uid, component);

        if (TryComp(uid, out RefillableSolutionComponent? refillComp))
            refillComp.Solution = component.Solution;

        if (TryComp(uid, out DrainableSolutionComponent? drainComp))
            drainComp.Solution = component.Solution;
    }

    private void OnSolutionChange(EntityUid uid, DrinkComponent component, SolutionChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    public void UpdateAppearance(EntityUid uid, DrinkComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !HasComp<SolutionContainerManagerComponent>(uid))
        {
            return;
        }

        var drainAvailable = DrinkVolume(uid, component);
        _appearance.SetData(uid, FoodVisuals.Visual, drainAvailable.Float(), appearance);
    }

    private bool TryDrink(EntityUid user, EntityUid target, DrinkComponent drink, EntityUid item)
    {
        if (!HasComp<BodyComponent>(target))
            return false;

        if (_openable.IsClosed(item, user))
            return true;

        if (!_solutionContainer.TryGetSolution(item, drink.Solution, out var drinkSolution) ||
            drinkSolution.Volume <= 0)
        {
            if (drink.IgnoreEmpty)
                return false;

            _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-is-empty", ("entity", item)), item, user);
            return true;
        }

        if (drinkSolution.Name == null)
            return false;

        if (_food.IsMouthBlocked(target, user))
            return true;

        if (!_interaction.InRangeUnobstructed(user, item, popup: true))
            return true;

        var forceDrink = user != target;

        if (forceDrink)
        {
            var userName = Identity.Entity(user, EntityManager);

            _popup.PopupEntity(Loc.GetString("drink-component-force-feed", ("user", userName)), user, target);

            // logging
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to drink {ToPrettyString(item):drink} {SolutionContainerSystem.ToPrettyString(drinkSolution)}");
        }
        else
        {
            // log voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is drinking {ToPrettyString(item):drink} {SolutionContainerSystem.ToPrettyString(drinkSolution)}");
        }

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(user, drinkSolution);

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            user,
            forceDrink ? drink.ForceFeedDelay : drink.Delay,
            new ConsumeDoAfterEvent(drinkSolution.Name, flavors),
            eventTarget: item,
            target: target,
            used: item)
        {
            BreakOnUserMove = forceDrink,
            BreakOnDamage = true,
            BreakOnTargetMove = forceDrink,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            // Mice and the like can eat without hands.
            // TODO maybe set this based on some CanEatWithoutHands event or component?
            NeedHand = forceDrink,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    /// <summary>
    ///     Raised directed at a victim when someone has force fed them a drink.
    /// </summary>
    private void OnDoAfter(EntityUid uid, DrinkComponent component, ConsumeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || component.Deleted)
            return;

        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;

        if (!_solutionContainer.TryGetSolution(args.Used, args.Solution, out var solution))
            return;

        // TODO this should really be checked every tick.
        if (_food.IsMouthBlocked(args.Target.Value))
            return;

        // TODO this should really be checked every tick.
        if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value))
            return;

        var transferAmount = FixedPoint2.Min(component.TransferAmount, solution.Volume);
        var drained = _solutionContainer.SplitSolution(uid, solution, transferAmount);
        var forceDrink = args.User != args.Target;

        args.Handled = true;
        if (transferAmount <= 0)
            return;

        if (!_body.TryGetBodyOrganComponents<StomachComponent>(args.Target.Value, out var stomachs, body))
        {
            _popup.PopupEntity(Loc.GetString(forceDrink ? "drink-component-try-use-drink-cannot-drink-other" : "drink-component-try-use-drink-had-enough"), args.Target.Value, args.User);

            if (HasComp<RefillableSolutionComponent>(args.Target.Value))
            {
                _puddle.TrySpillAt(args.User, drained, out _);
                return;
            }

            _solutionContainer.Refill(args.Target.Value, solution, drained);
            return;
        }

        var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Comp.Owner, drained));

        //All stomachs are full or can't handle whatever solution we have.
        if (firstStomach == null)
        {
            _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), args.Target.Value, args.Target.Value);

            if (forceDrink)
            {
                _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough-other"), args.Target.Value, args.User);
                _puddle.TrySpillAt(args.Target.Value, drained, out _);
            }
            else
                _solutionContainer.TryAddSolution(uid, solution, drained);

            return;
        }

        var flavors = args.FlavorMessage;

        if (forceDrink)
        {
            var targetName = Identity.Entity(args.Target.Value, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);

            _popup.PopupEntity(Loc.GetString("drink-component-force-feed-success", ("user", userName), ("flavors", flavors)), args.Target.Value, args.Target.Value);

            _popup.PopupEntity(
                Loc.GetString("drink-component-force-feed-success-user", ("target", targetName)),
                args.User, args.User);

            // log successful forced drinking
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(uid):user} forced {ToPrettyString(args.User):target} to drink {ToPrettyString(uid):drink}");
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("drink-component-try-use-drink-success-slurp-taste", ("flavors", flavors)), args.User,
                args.User);
            _popup.PopupEntity(
                Loc.GetString("drink-component-try-use-drink-success-slurp"), args.User, Filter.PvsExcept(args.User), true);

            // log successful voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} drank {ToPrettyString(uid):drink}");
        }

        _audio.PlayPvs(component.UseSound, args.Target.Value, AudioParams.Default.WithVolume(-2f));

        _reaction.DoEntityReaction(args.Target.Value, solution, ReactionMethod.Ingestion);
        //TODO: Grab the stomach UIDs somehow without using Owner
        _stomach.TryTransferSolution(firstStomach.Value.Comp.Owner, drained, firstStomach.Value.Comp);

        var comp = EnsureComp<ForensicsComponent>(uid);
        if (TryComp<DnaComponent>(args.Target, out var dna))
            comp.DNAs.Add(dna.DNA);

        if (!forceDrink && solution.Volume > 0)
            args.Repeat = true;
    }

    private void AddDrinkVerb(EntityUid uid, DrinkComponent component, GetVerbsEvent<AlternativeVerb> ev)
    {
        if (uid == ev.User ||
            !ev.CanInteract ||
            !ev.CanAccess ||
            !TryComp<BodyComponent>(ev.User, out var body) ||
            !_body.TryGetBodyOrganComponents<StomachComponent>(ev.User, out var stomachs, body))
            return;

        // no drinking from living drinks, have to kill them first.
        if (_mobState.IsAlive(uid))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryDrink(ev.User, ev.User, component, uid);
            },
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/drink.svg.192dpi.png")),
            Text = Loc.GetString("drink-system-verb-drink"),
            Priority = 2
        };

        ev.Verbs.Add(verb);
    }

    // some see half empty, and others see half full
    private string HalfEmptyOrHalfFull(ExaminedEvent args)
    {
        string remainingString = "drink-component-on-examine-is-half-full";

        if (TryComp<MetaDataComponent>(args.Examiner, out var examiner) && examiner.EntityName.Length > 0
            && string.Compare(examiner.EntityName.Substring(0, 1), "m", StringComparison.InvariantCultureIgnoreCase) > 0)
            remainingString = "drink-component-on-examine-is-half-empty";

        return remainingString;
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Lathe.Components;
using Content.Server.Materials;
using Content.Server.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Power;
using Content.Shared.ReagentSpeed;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._NF.Lathe;

namespace Content.Server._NF.Lathe;

[UsedImplicitly]
public sealed class BlueprintLatheSystem : SharedBlueprintLatheSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly ReagentSpeedSystem _reagentSpeed = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    /// <summary>
    /// Per-tick cache
    /// </summary>
    private readonly HashSet<ProtoId<LatheRecipePrototype>> _availableRecipes = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlueprintLatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
        SubscribeLocalEvent<BlueprintLatheComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlueprintLatheComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<BlueprintLatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
        SubscribeLocalEvent<BlueprintLatheComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);

        SubscribeLocalEvent<BlueprintLatheComponent, LatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
        SubscribeLocalEvent<BlueprintLatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);

        SubscribeLocalEvent<BlueprintLatheComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState(u, c));
        SubscribeLocalEvent<BlueprintLatheComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
        SubscribeLocalEvent<TechnologyDatabaseComponent, BlueprintLatheGetRecipesEvent>(OnGetRecipes);

        SubscribeLocalEvent<BlueprintLatheComponent, RefreshPartsEvent>(OnPartsRefresh);
        SubscribeLocalEvent<BlueprintLatheComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LatheProducingComponent, BlueprintLatheComponent>();
        while (query.MoveNext(out var uid, out var comp, out var lathe))
        {
            if (lathe.CurrentRecipe == null)
                continue;

            if (_timing.CurTime - comp.StartTime >= comp.ProductionLength)
                FinishProducing(uid, lathe);
        }
    }

    private void OnGetWhitelist(EntityUid uid, BlueprintLatheComponent component, ref GetMaterialWhitelistEvent args)
    {
        if (args.Storage != uid)
            return;

        args.Whitelist = args.Whitelist.Union(component.BlueprintPrintMaterials.Keys).ToList();
    }

    [PublicAPI]
    public bool TryGetAvailableRecipes(EntityUid uid, [NotNullWhen(true)] out List<ProtoId<LatheRecipePrototype>>? recipes, [NotNullWhen(true)] BlueprintLatheComponent? component = null, bool getUnavailable = false)
    {
        recipes = null;
        if (!Resolve(uid, ref component))
            return false;
        recipes = GetAvailableRecipes(uid, component, getUnavailable);
        return true;
    }

    public List<ProtoId<LatheRecipePrototype>> GetAvailableRecipes(EntityUid uid, BlueprintLatheComponent component, bool getUnavailable = false)
    {
        _availableRecipes.Clear();
        var ev = new BlueprintLatheGetRecipesEvent(uid, getUnavailable)
        {
            Recipes = _availableRecipes
        };
        RaiseLocalEvent(uid, ev);
        return [.. ev.Recipes];
    }

    public bool TryAddToQueue(EntityUid uid, LatheRecipePrototype recipe, int quantity, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (quantity <= 0)
            return false;

        if (!CanProduce(uid, recipe, quantity, component))
            return false;

        foreach (var (mat, amount) in component.BlueprintPrintMaterials)
        {
            var adjustedAmount = recipe.ApplyMaterialDiscount
                ? (int)(-amount * component.FinalMaterialUseMultiplier)
                : -amount;
            adjustedAmount *= quantity;

            _materialStorage.TryChangeMaterialAmount(uid, mat, adjustedAmount);
        }

        // Queue up a batch
        if (component.Queue.Count > 0 && component.Queue[^1].Recipe.ID == recipe.ID)
            component.Queue[^1].ItemsRequested += quantity;
        else
            component.Queue.Add(new LatheRecipeBatch(recipe, 0, quantity));

        return true;
    }

    public bool TryStartProducing(EntityUid uid, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (component.CurrentRecipe != null || component.Queue.Count <= 0 || !this.IsPowered(uid, EntityManager))
            return false;

        // handle batches
        var batch = component.Queue.First();
        batch.ItemsPrinted++;
        if (batch.ItemsPrinted >= batch.ItemsRequested || batch.ItemsPrinted < 0) // Rollover sanity check
            component.Queue.RemoveAt(0);
        var recipe = batch.Recipe;

        var time = _reagentSpeed.ApplySpeed(uid, component.BlueprintPrintTime) * component.TimeMultiplier;

        var lathe = EnsureComp<LatheProducingComponent>(uid);
        lathe.StartTime = _timing.CurTime;
        lathe.ProductionLength = time * component.FinalTimeMultiplier;
        component.CurrentRecipe = recipe;

        var ev = new LatheStartPrintingEvent(recipe);
        RaiseLocalEvent(uid, ref ev);

        _audio.PlayPvs(component.ProducingSound, uid);
        UpdateRunningAppearance(uid, true);
        UpdateUserInterfaceState(uid, component);

        if (time == TimeSpan.Zero)
        {
            FinishProducing(uid, component, lathe);
        }
        return true;
    }

    public void FinishProducing(EntityUid uid, BlueprintLatheComponent? comp = null, LatheProducingComponent? prodComp = null)
    {
        if (!Resolve(uid, ref comp, ref prodComp, false))
            return;

        if (comp.CurrentRecipe != null)
        {
            if (comp.CurrentRecipe.Result is { } resultProto)
            {
                var blueprint = Spawn("NFBlueprintPrinted", Transform(uid).Coordinates);
                if (TryComp<BlueprintComponent>(blueprint, out var blueprintComp))
                {
                    blueprintComp.ProvidedRecipes.Add(resultProto.Id);
                }
                _meta.SetEntityName(blueprint, GetRecipeName(comp.CurrentRecipe));
                _meta.SetEntityDescription(blueprint, GetRecipeDescription(comp.CurrentRecipe));
            }
        }

        comp.CurrentRecipe = null;
        prodComp.StartTime = _timing.CurTime;

        if (!TryStartProducing(uid, comp))
        {
            RemCompDeferred(uid, prodComp);
            UpdateUserInterfaceState(uid, comp);
            UpdateRunningAppearance(uid, false);
        }
    }

    public void UpdateUserInterfaceState(EntityUid uid, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var producing = component.CurrentRecipe ?? component.Queue.FirstOrDefault()?.Recipe; // Frontier: add ?.Recipe

        var state = new LatheUpdateState(GetAvailableRecipes(uid, component), component.Queue, producing);
        _uiSys.SetUiState(uid, LatheUiKey.Key, state);
    }

    /// <summary>
    /// Adds every unlocked recipe from each pack to the recipes list.
    /// </summary>
    public void AddDynamicRecipes(ref BlueprintLatheGetRecipesEvent args, TechnologyDatabaseComponent database)
    {
        if (args.GetUnavailable)
        {
            foreach (var recipe in PrintableRecipes)
                args.Recipes.Add(recipe);
        }
        else
        {
            foreach (var recipe in database.UnlockedRecipes)
                args.Recipes.Add(recipe);
        }
    }

    private void OnGetRecipes(EntityUid uid, TechnologyDatabaseComponent component, BlueprintLatheGetRecipesEvent args)
    {
        if (uid != args.Lathe || !HasComp<BlueprintLatheComponent>(uid))
            return;

        AddDynamicRecipes(ref args, component);
    }

    private void OnMaterialAmountChanged(EntityUid uid, BlueprintLatheComponent component, ref MaterialAmountChangedEvent args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    /// <summary>
    /// Initialize the UI and appearance.
    /// Appearance requires initialization or the layers break
    /// </summary>
    private void OnMapInit(EntityUid uid, BlueprintLatheComponent component, MapInitEvent args)
    {
        _appearance.SetData(uid, LatheVisuals.IsInserting, false);
        _appearance.SetData(uid, LatheVisuals.IsRunning, false);

        _materialStorage.UpdateMaterialWhitelist(uid);

        component.FinalTimeMultiplier = component.TimeMultiplier;
        component.FinalMaterialUseMultiplier = component.MaterialUseMultiplier;
    }

    /// <summary>
    /// Sets the machine sprite to either play the running animation
    /// or stop.
    /// </summary>
    private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
    {
        _appearance.SetData(uid, LatheVisuals.IsRunning, isRunning);
    }

    private void OnPowerChanged(EntityUid uid, BlueprintLatheComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            RemComp<LatheProducingComponent>(uid);
            UpdateRunningAppearance(uid, false);
        }
        else if (component.CurrentRecipe != null)
        {
            EnsureComp<LatheProducingComponent>(uid);
            TryStartProducing(uid, component);
        }
    }

    private void OnDatabaseModified(EntityUid uid, BlueprintLatheComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    private void OnResearchRegistrationChanged(EntityUid uid, BlueprintLatheComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    protected override bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, BlueprintLatheComponent component)
    {
        return GetAvailableRecipes(uid, component).Contains(recipe.ID);
    }

    #region UI Messages

    private void OnLatheQueueRecipeMessage(EntityUid uid, BlueprintLatheComponent component, LatheQueueRecipeMessage args)
    {
        if (_proto.TryIndex(args.ID, out LatheRecipePrototype? recipe))
        {
            if (TryAddToQueue(uid, recipe, args.Quantity, component))
            {
                _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} queued {args.Quantity} {GetRecipeName(recipe)} at {ToPrettyString(uid):lathe}");
            }
        }
        TryStartProducing(uid, component);
        UpdateUserInterfaceState(uid, component);
    }

    private void OnLatheSyncRequestMessage(EntityUid uid, BlueprintLatheComponent component, LatheSyncRequestMessage args)
    {
        UpdateUserInterfaceState(uid, component);
    }
    #endregion

    private void OnPartsRefresh(EntityUid uid, BlueprintLatheComponent component, RefreshPartsEvent args)
    {
        var printTimeRating = args.PartRatings[component.MachinePartPrintSpeed];
        var materialUseRating = args.PartRatings[component.MachinePartMaterialUse];

        component.FinalTimeMultiplier = component.TimeMultiplier * MathF.Pow(component.PartRatingPrintTimeMultiplier, printTimeRating - 1);
        component.FinalMaterialUseMultiplier = component.MaterialUseMultiplier * MathF.Pow(component.PartRatingMaterialUseMultiplier, materialUseRating - 1);
        Dirty(uid, component);
    }

    private void OnUpgradeExamine(EntityUid uid, BlueprintLatheComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("lathe-component-upgrade-speed", 1 / component.FinalTimeMultiplier);
        args.AddPercentageUpgrade("lathe-component-upgrade-material-use", component.FinalMaterialUseMultiplier);
    }
}

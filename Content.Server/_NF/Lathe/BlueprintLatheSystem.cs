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
using Content.Shared._NF.Research.Prototypes;

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

    /// <summary>
    /// Per-tick cache
    /// </summary>
    private readonly Dictionary<ProtoId<BlueprintPrototype>, int[]> _availableRecipes = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlueprintLatheComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
        SubscribeLocalEvent<BlueprintLatheComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlueprintLatheComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<BlueprintLatheComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
        SubscribeLocalEvent<BlueprintLatheComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);

        SubscribeLocalEvent<BlueprintLatheComponent, BlueprintLatheQueueRecipeMessage>(OnLatheQueueRecipeMessage);
        SubscribeLocalEvent<BlueprintLatheComponent, LatheSyncRequestMessage>(OnLatheSyncRequestMessage);
        SubscribeLocalEvent<BlueprintLatheComponent, LatheDeleteRequestMessage>(OnLatheDeleteRequestMessage);
        SubscribeLocalEvent<BlueprintLatheComponent, LatheMoveRequestMessage>(OnLatheMoveRequestMessage);
        SubscribeLocalEvent<BlueprintLatheComponent, LatheAbortFabricationMessage>(OnLatheAbortFabricationMessage);

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
            if (lathe.CurrentBlueprintType == null)
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
    public bool TryGetAvailableRecipes(EntityUid uid, [NotNullWhen(true)] out Dictionary<ProtoId<BlueprintPrototype>, int[]>? recipes, [NotNullWhen(true)] BlueprintLatheComponent? component = null)
    {
        recipes = null;
        if (!Resolve(uid, ref component))
            return false;
        recipes = GetAvailableRecipes(uid);
        return true;
    }

    public Dictionary<ProtoId<BlueprintPrototype>, int[]> GetAvailableRecipes(EntityUid uid)
    {
        _availableRecipes.Clear();
        var ev = new BlueprintLatheGetRecipesEvent(uid)
        {
            UnlockedRecipes = _availableRecipes
        };
        RaiseLocalEvent(uid, ev);
        return _availableRecipes;
    }

    public bool TryAddToQueue(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, int[] recipes, int quantity, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (quantity <= 0)
            return false;

        if (!CanProduce(uid, blueprintType, recipes, quantity, component))
            return false;

        foreach (var (mat, amount) in component.BlueprintPrintMaterials)
        {
            var adjustedAmount = component.ApplyMaterialDiscount
                ? (int)(-amount * component.FinalMaterialUseMultiplier)
                : -amount;
            adjustedAmount *= quantity;

            _materialStorage.TryChangeMaterialAmount(uid, mat, adjustedAmount);
        }

        // Queue up a batch
        if (component.Queue.Count > 0 && component.Queue[^1].Recipes == recipes)
            component.Queue[^1].ItemsRequested += quantity;
        else
            component.Queue.Add(new BlueprintLatheRecipeBatch(blueprintType, recipes, 0, quantity));

        return true;
    }

    public bool TryStartProducing(EntityUid uid, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (component.CurrentBlueprintType != null || component.Queue.Count <= 0 || !this.IsPowered(uid, EntityManager))
            return false;

        // handle batches
        var batch = component.Queue.First();
        batch.ItemsPrinted++;
        if (batch.ItemsPrinted >= batch.ItemsRequested || batch.ItemsPrinted < 0) // Rollover sanity check
            component.Queue.RemoveAt(0);
        var blueprintType = batch.BlueprintType;

        var time = _reagentSpeed.ApplySpeed(uid, component.BlueprintPrintTime) * component.TimeMultiplier * component.FinalTimeMultiplier;

        var lathe = EnsureComp<LatheProducingComponent>(uid);
        lathe.StartTime = _timing.CurTime;
        lathe.ProductionLength = time;
        component.CurrentBlueprintType = blueprintType;
        component.CurrentRecipeSets = batch.Recipes;

        _audio.PlayPvs(component.ProducingSound, uid);
        UpdateRunningAppearance(uid, true);
        UpdateUserInterfaceState(uid, component);

        if (time <= TimeSpan.Zero)
        {
            FinishProducing(uid, component, lathe);
        }
        return true;
    }

    public void FinishProducing(EntityUid uid, BlueprintLatheComponent? comp = null, LatheProducingComponent? prodComp = null)
    {
        if (!Resolve(uid, ref comp, ref prodComp, false))
            return;

        if (comp.CurrentBlueprintType != null
            && comp.CurrentRecipeSets != null
            && _proto.TryIndex(comp.CurrentBlueprintType, out var blueprintProto)
            && PrintableRecipesByType.TryGetValue(comp.CurrentBlueprintType.Value, out var possibleRecipes))
        {
            var blueprint = Spawn(blueprintProto.Blueprint, Transform(uid).Coordinates);
            var blueprintComp = EnsureComp<BlueprintComponent>(blueprint);

            bool anyRecipes = false;
            for (int i = 0; i < possibleRecipes.Count; i++)
            {
                int idx = i / 32;
                int value = 1 << (i % 32);

                if (idx >= possibleRecipes.Count)
                    break;

                if ((comp.CurrentRecipeSets[idx] & value) != 0)
                {
                    blueprintComp.ProvidedRecipes.Add(possibleRecipes[i]);
                    anyRecipes = true;
                }
            }

            if (anyRecipes)
                Dirty(blueprint, blueprintComp);
            else
                QueueDel(blueprint);
        }

        comp.CurrentBlueprintType = null;
        comp.CurrentRecipeSets = null;
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

        var producing = component.CurrentBlueprintType ?? component.Queue.FirstOrDefault()?.BlueprintType;

        var state = new BlueprintLatheUpdateState(GetAvailableRecipes(uid), component.Queue, producing);
        _uiSys.SetUiState(uid, BlueprintLatheUiKey.Key, state);
    }

    /// <summary>
    /// Adds every unlocked recipe from each pack to the recipes list.
    /// </summary>
    public void AddDynamicRecipes(ref BlueprintLatheGetRecipesEvent args, TechnologyDatabaseComponent database)
    {
        // Setup bitsets
        foreach (var blueprintType in _proto.EnumeratePrototypes<BlueprintPrototype>())
        {
            if (PrintableRecipesByType.TryGetValue(blueprintType.ID, out var list))
            {
                int numbersNeeded = (list.Count + 31) / 32;
                args.UnlockedRecipes.Add(blueprintType.ID, new int[numbersNeeded]);
            }
            else
                args.UnlockedRecipes.Add(blueprintType.ID, Array.Empty<int>());
        }

        foreach (var recipe in database.UnlockedRecipes)
        {
            if (PrintableRecipes.TryGetValue(recipe, out var value))
            {
                foreach (var recipeInfo in value)
                {
                    var index = recipeInfo.index / 32;
                    var intValue = 1 << (recipeInfo.index % 32);
                    args.UnlockedRecipes[recipeInfo.blueprint][index] |= intValue;
                }
            }
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
            AbortProduction(uid);
        else
            TryStartProducing(uid, component);
    }

    private void OnDatabaseModified(EntityUid uid, BlueprintLatheComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    private void OnResearchRegistrationChanged(EntityUid uid, BlueprintLatheComponent component, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    protected override bool HasRecipes(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, int[] requestedRecipes, BlueprintLatheComponent component)
    {
        if (!PrintableRecipesByType.TryGetValue(blueprintType, out _))
            return false;

        var availableRecipesByType = GetAvailableRecipes(uid);
        if (!availableRecipesByType.TryGetValue(blueprintType, out var availableRecipes))
            return false;

        // If our recipe array is longer than the available recipes, if we're asking for any recipes beyond those bounds, this should fail.
        for (int i = availableRecipes.Length; i < requestedRecipes.Length; i++)
        {
            if (requestedRecipes[i] != 0)
                return false;
        }

        // For each set, compare what we want against what we have.
        // An empty requested set should fail (you can't print an empty blueprint).
        bool anythingRequested = false;
        for (int i = 0; i < Math.Min(requestedRecipes.Length, availableRecipes.Length); i++)
        {
            if (requestedRecipes[i] != 0)
            {
                anythingRequested = true;
                if ((requestedRecipes[i] & ~availableRecipes[i]) != 0)
                    return false;
            }
        }

        return anythingRequested;
    }

    protected override bool HasRecipe(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, ProtoId<LatheRecipePrototype> recipe, BlueprintLatheComponent component)
    {
        if (!PrintableRecipes.TryGetValue(recipe, out var recipeInfo))
            return false;

        int? maybeIndex = null;
        foreach (var recipeBlueprintType in recipeInfo)
        {
            if (recipeBlueprintType.blueprint == blueprintType)
            {
                maybeIndex = recipeBlueprintType.index;
                break;
            }
        }

        if (maybeIndex is not { } index)
            return false;

        var recipeDict = GetAvailableRecipes(uid);
        if (!recipeDict.TryGetValue(blueprintType, out var intArray)
            || intArray.Length < (index + 31) / 32)
        {
            return false;
        }
        return (intArray[index / 32] & (1 << (index % 32))) != 0;
    }

    #region UI Messages

    private void OnLatheQueueRecipeMessage(EntityUid uid, BlueprintLatheComponent component, BlueprintLatheQueueRecipeMessage args)
    {
        if (_proto.TryIndex(args.BlueprintType, out BlueprintPrototype? recipe)
            && TryAddToQueue(uid, recipe.ID, args.Recipes, args.Quantity, component))
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} queued {args.Quantity} at {ToPrettyString(uid):lathe}");
        }
        TryStartProducing(uid, component);
        UpdateUserInterfaceState(uid, component);
    }

    private void OnLatheSyncRequestMessage(EntityUid uid, BlueprintLatheComponent component, LatheSyncRequestMessage args)
    {
        UpdateUserInterfaceState(uid, component);
    }

    public void AbortProduction(EntityUid uid, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (component.CurrentBlueprintType != null && component.CurrentRecipeSets != null)
        {
            // Items incremented on start, need to decrement with removal
            if (component.Queue.Count > 0)
            {
                var batch = component.Queue.First();
                if (batch.BlueprintType != component.CurrentBlueprintType && RecipeSetsEqual(batch.Recipes, component.CurrentRecipeSets))
                {
                    var newBatch = new BlueprintLatheRecipeBatch(component.CurrentBlueprintType.Value, component.CurrentRecipeSets, 0, 1);
                    component.Queue.Insert(0, newBatch);
                }
                else if (batch.ItemsPrinted > 0)
                {
                    batch.ItemsPrinted--;
                }
            }
        }
        component.CurrentBlueprintType = null;
        component.CurrentRecipeSets = null;
        RemCompDeferred<LatheProducingComponent>(uid);
        UpdateUserInterfaceState(uid, component);
        UpdateRunningAppearance(uid, false);
    }

    public bool RecipeSetsEqual(int[] left, int[] right)
    {
        var minLength = Math.Min(left.Length, right.Length);
        // Compare common elements
        for (int i = 0; i < minLength; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        // Extents: must be all zero to be equal.
        if (left.Length > right.Length)
        {
            for (int i = minLength; i < left.Length; i++)
            {
                if (left[i] != 0)
                    return false;
            }
        }
        else
        {
            for (int i = minLength; i < right.Length; i++)
            {
                if (right[i] != 0)
                    return false;
            }
        }
        return true;
    }

    public void OnLatheDeleteRequestMessage(EntityUid uid, BlueprintLatheComponent component, ref LatheDeleteRequestMessage args)
    {
        if (args.Index < 0 || args.Index >= component.Queue.Count)
            return;

        var batch = component.Queue[args.Index];
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} deleted a lathe job for ({batch.ItemsPrinted}/{batch.ItemsRequested}) {component.CurrentBlueprintType} blueprints at {ToPrettyString(uid):lathe}");

        component.Queue.RemoveAt(args.Index);
        UpdateUserInterfaceState(uid, component);
    }

    public void OnLatheMoveRequestMessage(EntityUid uid, BlueprintLatheComponent component, ref LatheMoveRequestMessage args)
    {
        if (args.Change == 0 || args.Index < 0 || args.Index >= component.Queue.Count)
            return;

        var newIndex = args.Index + args.Change;
        if (newIndex < 0 || newIndex >= component.Queue.Count)
            return;

        var temp = component.Queue[args.Index];
        component.Queue[args.Index] = component.Queue[newIndex];
        component.Queue[newIndex] = temp;
        UpdateUserInterfaceState(uid, component);
    }

    public void OnLatheAbortFabricationMessage(EntityUid uid, BlueprintLatheComponent component, ref LatheAbortFabricationMessage args)
    {
        if (component.CurrentBlueprintType == null && component.CurrentRecipeSets == null)
            return;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} aborted printing a {component.CurrentBlueprintType} blueprint at {ToPrettyString(uid):lathe}");

        component.CurrentBlueprintType = null;
        component.CurrentRecipeSets = null;
        FinishProducing(uid, component);
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

    /// <summary>
    /// Accesssor to set blueprints server-side.
    /// </summary>
    /// <param name="ent">Blueprint to alter.</param>
    /// <param name="reipces">The recipe set the blueprint should unlock.</param>
    /// <remarks>
    /// This is in the BlueprintLatheSystem as BlueprintSystem is sealed, I don't particularly want to edit it.
    /// </remarks>
    public void SetBlueprintRecipes(Entity<BlueprintComponent> ent, HashSet<ProtoId<LatheRecipePrototype>> recipes)
    {
        ent.Comp.ProvidedRecipes = recipes;
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Adds a given recipe to a blueprint.
    /// </remarks>
    public void AddBlueprintRecipe(Entity<BlueprintComponent> ent, ProtoId<LatheRecipePrototype> recipe, bool dirty = true)
    {
        var inserted = ent.Comp.ProvidedRecipes.Add(recipe);
        if (inserted && dirty)
            Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Removes a given recipe from a blueprint.
    /// </remarks>
    public void RemoveBlueprintRecipe(Entity<BlueprintComponent> ent, ProtoId<LatheRecipePrototype> recipe, bool dirty = true)
    {
        var removed = ent.Comp.ProvidedRecipes.Remove(recipe);
        if (removed && dirty)
            Dirty(ent, ent.Comp);
    }
}

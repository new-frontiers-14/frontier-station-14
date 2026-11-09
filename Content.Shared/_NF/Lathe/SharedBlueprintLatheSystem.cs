using Content.Shared._NF.Research.Prototypes;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Lathe;

/// <summary>
/// This handles printing blueprints from all technologies known to a technology database.
/// </summary>
public abstract class SharedBlueprintLatheSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    /// <summary>
    /// A lookup table of all printable recipes and the blueprint types they can be printed as.
    /// </summary>
    public readonly Dictionary<ProtoId<LatheRecipePrototype>, List<(ProtoId<BlueprintPrototype> blueprint, int index)>> PrintableRecipes = new();

    /// <summary>
    /// A lookup table of all printable blueprint types and each recipe that prints as that type.
    /// Each list must be sorted alphabetically, and these indices are used as indices in a bitset in print requests.
    /// </summary>
    public readonly Dictionary<ProtoId<BlueprintPrototype>, List<ProtoId<LatheRecipePrototype>>> PrintableRecipesByType = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        BuildBlueprintRecipeList();
    }

    [PublicAPI]
    public bool CanProduce(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, int[] recipe, int amount = 1, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // TODO: should we reduce the set of recipes down to what we do have (and fail on empty) if this asks for things we don't have vs. failing?
        if (!HasRecipes(uid, blueprintType, recipe, component))
            return false;

        return HasBlueprintMaterial(uid, amount, component);
    }

    [PublicAPI]
    public bool HasBlueprintMaterial(EntityUid uid, int amount = 1, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        foreach (var (material, needed) in component.BlueprintPrintMaterials)
        {
            var adjustedAmount = AdjustMaterial(needed, component.ApplyMaterialDiscount, component.FinalMaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(uid, material) < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    [PublicAPI]
    public bool CanProduceRecipe(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, ProtoId<LatheRecipePrototype> recipe, int amount = 1, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!HasRecipe(uid, blueprintType, recipe, component))
            return false;

        foreach (var (material, needed) in component.BlueprintPrintMaterials)
        {
            var adjustedAmount = AdjustMaterial(needed, component.ApplyMaterialDiscount, component.FinalMaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(uid, material) < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int)MathF.Ceiling(original * multiplier) : original;

    protected abstract bool HasRecipes(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, int[] recipe, BlueprintLatheComponent component);
    protected abstract bool HasRecipe(EntityUid uid, ProtoId<BlueprintPrototype> blueprintType, ProtoId<LatheRecipePrototype> recipe, BlueprintLatheComponent component);

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<BlueprintPrototype>())
            return;
        BuildBlueprintRecipeList();
    }

    private void BuildBlueprintRecipeList()
    {
        PrintableRecipes.Clear();
        PrintableRecipesByType.Clear();

        // Set up collections
        foreach (var blueprintProto in _proto.EnumeratePrototypes<BlueprintPrototype>())
        {
            List<ProtoId<LatheRecipePrototype>> recipeList = new();

            // Fill in collections from packs
            foreach (var pack in blueprintProto.Packs)
            {
                if (!_proto.TryIndex(pack, out var packProto))
                    continue;

                foreach (var recipe in packProto.Recipes)
                {
                    if (!_proto.HasIndex(recipe))
                        continue;

                    recipeList.Add(recipe);
                }
            }
            PrintableRecipesByType.Add(blueprintProto.ID, recipeList);
        }

        // Associate each recipe with blueprint keys and indices
        foreach (var (blueprintType, recipeList) in PrintableRecipesByType)
        {
            // Set up index values
            int index = 0;
            foreach (var recipe in recipeList)
            {
                if (!PrintableRecipes.TryGetValue(recipe, out var blueprintList))
                {
                    PrintableRecipes.Add(recipe, new());
                    blueprintList = PrintableRecipes[recipe];
                }
                blueprintList.Add((blueprintType, index));
                index++;
            }
        }
    }

    public string GetRecipeName(ProtoId<LatheRecipePrototype> proto)
    {
        return GetRecipeName(_proto.Index(proto));
    }

    public string GetRecipeName(LatheRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Name))
            return Loc.GetString(proto.Name);

        if (proto.Result is { } result)
            return Loc.GetString("blueprint-lathe-name", ("name", _proto.Index(result).Name));

        return string.Empty;
    }

    [PublicAPI]
    public string GetRecipeDescription(ProtoId<LatheRecipePrototype> proto)
    {
        return GetRecipeDescription(_proto.Index(proto));
    }

    public string GetRecipeDescription(LatheRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Description))
            return Loc.GetString(proto.Description);

        if (proto.Result is { } result)
            return Loc.GetString("blueprint-lathe-description", ("name", _proto.Index(result).Name));

        return string.Empty;
    }
}

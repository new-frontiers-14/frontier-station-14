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

    public readonly HashSet<ProtoId<LatheRecipePrototype>> PrintableRecipes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        BuildBlueprintRecipeList();
    }

    [PublicAPI]
    public bool CanProduce(EntityUid uid, string recipe, int amount = 1, BlueprintLatheComponent? component = null)
    {
        return _proto.TryIndex<LatheRecipePrototype>(recipe, out var proto) && CanProduce(uid, proto, amount, component);
    }

    public bool CanProduce(EntityUid uid, LatheRecipePrototype recipe, int amount = 1, BlueprintLatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (!HasRecipe(uid, recipe, component))
            return false;

        foreach (var (material, needed) in component.BlueprintPrintMaterials)
        {
            var adjustedAmount = AdjustMaterial(needed, recipe.ApplyMaterialDiscount, component.FinalMaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(uid, material) < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int)MathF.Ceiling(original * multiplier) : original;

    protected abstract bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, BlueprintLatheComponent component);

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<TechnologyPrototype>())
            return;
        BuildBlueprintRecipeList();
    }

    private void BuildBlueprintRecipeList()
    {
        PrintableRecipes.Clear();
        foreach (var tech in _proto.EnumeratePrototypes<TechnologyPrototype>())
        {
            foreach (var recipe in tech.RecipeUnlocks)
                PrintableRecipes.Add(recipe);
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

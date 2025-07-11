using Content.Server._NF.Lathe;
using Content.Server._NF.Stacks.Components;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Research.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Research.Systems;

public sealed class RandomBlueprintSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BlueprintLatheSystem _blueprintLathe = default!;

    private readonly List<(int count, LatheRecipePackPrototype pack)> _packs = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomBlueprintComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<RandomBlueprintComponent> ent, ref ComponentInit init)
    {
        // Get list of recipes for given blueprint type
        if (!TryComp(ent, out BlueprintComponent? blueprintComp))
            return;

        if (!_proto.TryIndex(ent.Comp.Blueprint, out var blueprintProto))
            return;

        var rolls = _random.Next(ent.Comp.MinRolls, ent.Comp.MaxRolls + 1);
        if (rolls <= 0)
            return;

        var totalRecipes = 0;
        _packs.Clear();

        foreach (var pack in blueprintProto.Packs)
        {
            if (!_proto.TryIndex(pack, out var packProto))
                continue;

            _packs.Add((totalRecipes + packProto.Recipes.Count, packProto));
            totalRecipes += packProto.Recipes.Count;
        }

        // Early exit if no recipes available
        if (totalRecipes == 0)
            return;

        // Select random recipes from cached blueprints
        // Doing this naively - if you reroll the same recipe, tough luck.
        for (int i = 0; i < rolls; i++)
        {
            var recipeIndex = _random.Next(totalRecipes);

            var packStartIndex = 0;
            foreach (var packTuple in _packs)
            {
                if (recipeIndex < packTuple.count)
                {
                    // Find relative index in pack
                    var relativeIndex = recipeIndex - packStartIndex;
                    var currentIndex = 0;
                    foreach (var recipe in packTuple.pack.Recipes)
                    {
                        if (currentIndex == relativeIndex)
                        {
                            _blueprintLathe.AddBlueprintRecipe((ent, blueprintComp), recipe, false);
                            break;
                        }
                        currentIndex++;
                    }
                    break;
                }
                packStartIndex = packTuple.count;
            }
        }
        Dirty(ent, blueprintComp);
    }
}

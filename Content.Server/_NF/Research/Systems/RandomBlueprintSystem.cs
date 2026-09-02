using System.Linq;
using Content.Server._NF.Lathe;
using Content.Server._NF.Stacks.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Research.Systems;

public sealed class RandomBlueprintSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BlueprintLatheSystem _blueprintLathe = default!;
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

        HashSet<ProtoId<LatheRecipePrototype>> recipes = new();

        foreach (var pack in blueprintProto.Packs)
        {
            if (!_proto.TryIndex(pack, out var packProto))
                continue;

            recipes.UnionWith(packProto.Recipes);
        }

        var recipeList = recipes.ToList();
        if (recipeList.Count < rolls)
        {
            rolls = recipeList.Count;
        }

        if (rolls == 0)
            return;

        // Select random recipes from recipe list
        for (int i = 0; i < rolls; i++)
        {
            _blueprintLathe.AddBlueprintRecipe((ent, blueprintComp), _random.PickAndTake(recipeList), false);
        }
        Dirty(ent, blueprintComp);
    }
}

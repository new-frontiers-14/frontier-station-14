using System.Linq;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;
using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Client.Chemistry.EntitySystems;

public abstract class SharedFoodGuideDataSystem : EntitySystem
{
    public List<FoodGuideEntry> Registry = new();
}

[Serializable, NetSerializable]
public sealed class FoodGuideRegistryChangedEvent : EntityEventArgs
{
    [DataField]
    public List<FoodGuideEntry> Changeset;

    public FoodGuideRegistryChangedEvent(List<FoodGuideEntry> changeset)
    {
        Changeset = changeset;
    }
}

[DataDefinition, Serializable, NetSerializable]
public partial struct FoodGuideEntry
{
    [DataField]
    public EntProtoId Result;

    [DataField]
    public string Identifier; // Used for sorting

    [DataField] // Frontier
    public FoodSourceData[] Recipes; // Frontier

    [DataField]
    public FoodSourceData[] Sources;

    [DataField]
    public ReagentQuantity[] Composition;

    public FoodGuideEntry(EntProtoId result, string identifier, FoodSourceData[] recipes, FoodSourceData[] sources, ReagentQuantity[] composition) // Frontier: add recipes
    {
        Result = result;
        Identifier = identifier;
        Recipes = recipes; // Frontier
        Sources = sources;
        Composition = composition;
    }
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class FoodSourceData
{
    /// <summary>
    ///     Number of products created from this source. Used for primary ordering.
    /// </summary>
    public abstract int OutputCount { get; }

    /// <summary>
    ///     A string used to distinguish different sources. Typically the name of the related entity.
    /// </summary>
    public string Identitier;

    /// <summary>
    ///     Frontier: context about this food source - is it a recipe?  Is it a source?
    /// </summary>
    public FoodSourceType SourceType;

    public abstract bool IsSourceOf(EntProtoId food);
}

// Frontier: enum for differentiating sources
public enum FoodSourceType : byte
{
    Recipe = 0,
    Source = 1,
}
// End Frontier

[Serializable, NetSerializable]
public sealed partial class FoodButcheringData : FoodSourceData
{
    [DataField]
    public EntProtoId Butchered;

    [DataField]
    public ButcheringType Type;
    [DataField]
    public EntitySpawnEntry Result; // Frontier: List<EntitySpawnEntry><EntitySpawnEntry, Results<Result

    public override int OutputCount => Result.Amount; // Frontier: Results.Count<Result.Amount

    public FoodButcheringData(EntityPrototype butchered, ButcherableComponent comp, EntitySpawnEntry product) // Frontier: add product
    {
        Identitier = butchered.Name;
        Butchered = butchered.ID;
        Type = comp.Type;
        //Results = comp.SpawnedEntities; // Frontier: unused
        Result = product; // Frontier: Results<Result, comp.SpawnedEntities<product
        SourceType = FoodSourceType.Source;
        // End Frontier
    }

    //public override bool IsSourceOf(EntProtoId food) => Results.Any(it => it.PrototypeId == food); // Frontier
    public override bool IsSourceOf(EntProtoId food) => Result.PrototypeId == food.Id; // Frontier
}

[Serializable, NetSerializable]
public sealed partial class FoodSlicingData : FoodSourceData
{
    [DataField]
    public EntProtoId Sliced, Result;

    [DataField]
    private int _outputCount;
    public override int OutputCount => _outputCount;

    public FoodSlicingData(EntityPrototype sliced, EntProtoId result, int outputCount)
    {
        Identitier = sliced.Name;
        Sliced = sliced.ID;
        Result = result;
        _outputCount = outputCount; // Server-only
        SourceType = FoodSourceType.Source; // Frontier
    }

    public override bool IsSourceOf(EntProtoId food) => food == Result;
}

[Serializable, NetSerializable]
public sealed partial class FoodRecipeData : FoodSourceData
{
    [DataField]
    public ProtoId<FoodRecipePrototype> Recipe;

    [DataField]
    public EntProtoId Result;

    [DataField] // Frontier
    private int _outputCount; // Frontier
    public override int OutputCount => _outputCount; // Frontier: 1<ResultCount

    public FoodRecipeData(FoodRecipePrototype proto)
    {
        Identitier = proto.Name;
        Recipe = proto.ID;
        Result = proto.Result;
        _outputCount = proto.ResultCount; // Frontier
        SourceType = FoodSourceType.Recipe; // Frontier
    }

    public override bool IsSourceOf(EntProtoId food) => food == Result;
}

[Serializable, NetSerializable]
public sealed partial class FoodReactionData : FoodSourceData
{
    [DataField]
    public ProtoId<ReactionPrototype> Reaction;

    [DataField]
    public EntProtoId Result;

    [DataField]
    private int _outputCount;
    public override int OutputCount => _outputCount;

    public FoodReactionData(ReactionPrototype reaction, EntProtoId result, int outputCount)
    {
        Identitier = reaction.Name;
        Reaction = reaction.ID;
        Result = result;
        _outputCount = outputCount;
        SourceType = FoodSourceType.Recipe; // Frontier
    }

    public override bool IsSourceOf(EntProtoId food) => food == Result;
}

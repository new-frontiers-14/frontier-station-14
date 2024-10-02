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

    [DataField]
    public FoodSourceData[] Sources;

    [DataField]
    public ReagentQuantity[] Composition;

    public FoodGuideEntry(EntProtoId result, string identifier, FoodSourceData[] sources, ReagentQuantity[] composition)
    {
        Result = result;
        Identifier = identifier;
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

    public abstract bool IsSourceOf(EntProtoId food);
}

[Serializable, NetSerializable]
public sealed partial class FoodButcheringData : FoodSourceData
{
    [DataField]
    public EntProtoId Butchered;

    [DataField]
    public ButcheringType Type;

    [DataField]
    public List<EntitySpawnEntry> Results;

    public override int OutputCount => Results.Count;

    public FoodButcheringData(EntityPrototype butchered, ButcherableComponent comp)
    {
        Identitier = butchered.Name;
        Butchered = butchered.ID;
        Type = comp.Type;
        Results = comp.SpawnedEntities;
    }

    public override bool IsSourceOf(EntProtoId food) => Results.Any(it => it.PrototypeId == food);
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
    }

    public override bool IsSourceOf(EntProtoId food) => food == Result;
}

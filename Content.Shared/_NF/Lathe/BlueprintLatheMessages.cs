using Content.Shared._NF.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Lathe;

[Serializable, NetSerializable]
public sealed class BlueprintLatheUpdateState : BoundUserInterfaceState
{
    public Dictionary<ProtoId<BlueprintPrototype>, int[]> RecipeBitsetByBlueprintType;

    public List<BlueprintLatheRecipeBatch> Queue;

    public ProtoId<BlueprintPrototype>? CurrentlyProducing;

    public BlueprintLatheUpdateState(
        Dictionary<ProtoId<BlueprintPrototype>, int[]> recipeBitsetByBlueprintType,
        List<BlueprintLatheRecipeBatch> queue,
        ProtoId<BlueprintPrototype>? currentlyProducing = null
    )
    {
        RecipeBitsetByBlueprintType = recipeBitsetByBlueprintType;
        Queue = queue;
        CurrentlyProducing = currentlyProducing;
    }
}

/// <summary>
///     Sent to the server when a client queues a new recipe.
/// </summary>
[Serializable, NetSerializable]
public sealed class BlueprintLatheQueueRecipeMessage : BoundUserInterfaceMessage
{
    public readonly string BlueprintType;
    public readonly int[] Recipes;
    public readonly int Quantity;
    public BlueprintLatheQueueRecipeMessage(string blueprintType, int[] recipes, int quantity)
    {
        BlueprintType = blueprintType;
        Recipes = recipes;
        Quantity = quantity;
    }
}

[NetSerializable, Serializable]
public enum BlueprintLatheUiKey
{
    Key,
}

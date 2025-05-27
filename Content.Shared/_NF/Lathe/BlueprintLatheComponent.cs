using Content.Shared._NF.Research.Prototypes;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Lathe;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlueprintLatheComponent : Component
{
    /// <summary>
    /// The lathe's construction queue
    /// </summary>
    [DataField]
    public List<BlueprintLatheRecipeBatch> Queue = new();

    /// <summary>
    /// The sound that plays when the lathe is producing an item, if any
    /// </summary>
    [DataField]
    public SoundSpecifier? ProducingSound;

    /// <summary>
    /// The default amount that's displayed in the UI for selecting the print amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DefaultProductionAmount = 1;

    /// <summary>
    /// The materials required to make an individual blueprint
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<MaterialPrototype>, int> BlueprintPrintMaterials = new();

    /// <summary>
    /// The time required to print an individual blueprint
    /// </summary>
    [DataField(required: true)]
    public TimeSpan BlueprintPrintTime;

    /// <summary>
    /// If true, blueprints will receive a discount based on the quality of the components in the machine.
    /// </summary>
    [ViewVariables]
    public bool ApplyMaterialDiscount;

    #region Visualizer info
    [DataField]
    public string? IdleState;

    [DataField]
    public string? RunningState;

    [DataField]
    public string? UnlitIdleState;

    [DataField]
    public string? UnlitRunningState;
    #endregion

    /// <summary>
    /// The blueprint type the lathe is currently producing.
    /// </summary>
    [ViewVariables]
    public ProtoId<BlueprintPrototype>? CurrentBlueprintType;

    /// <summary>
    /// The recipe types the blueprint the lathe is currently producing.
    /// </summary>
    [ViewVariables]
    public int[]? CurrentRecipeSets;

    #region MachineUpgrading
    /// <summary>
    /// A modifier that changes how long it takes to print a recipe
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeMultiplier = 1;

    /// <summary>
    /// A modifier that changes how much of a material is needed to print a recipe
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaterialUseMultiplier = 1;

    /// <summary>
    /// A modifier that changes how long it takes to print a recipe
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float FinalTimeMultiplier = 1;

    /// <summary>
    /// A modifier that changes how much of a material is needed to print a recipe
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float FinalMaterialUseMultiplier = 1;

    public const float DefaultPartRatingMaterialUseMultiplier = 0.85f;

    /// <summary>
    /// The machine part that reduces how long it takes to print a recipe.
    /// </summary>
    [DataField]
    public ProtoId<MachinePartPrototype> MachinePartPrintSpeed = "Manipulator";

    /// <summary>
    /// The value that is used to calculate the modified <see cref="TimeMultiplier"/>
    /// </summary>
    [DataField]
    public float PartRatingPrintTimeMultiplier = 0.5f;

    /// <summary>
    /// The machine part that reduces how much material it takes to print a recipe.
    /// </summary>
    [DataField]
    public ProtoId<MachinePartPrototype> MachinePartMaterialUse = "MatterBin";

    /// <summary>
    /// The value that is used to calculate the modifier <see cref="MaterialUseMultiplier"/>
    /// </summary>
    [DataField]
    public float PartRatingMaterialUseMultiplier = DefaultPartRatingMaterialUseMultiplier;
    #endregion
}

[Serializable]
public sealed partial class BlueprintLatheRecipeBatch : EntityEventArgs
{
    public ProtoId<BlueprintPrototype> BlueprintType;
    public int[] Recipes;
    public int ItemsPrinted;
    public int ItemsRequested;

    public BlueprintLatheRecipeBatch(ProtoId<BlueprintPrototype> blueprintType, int[] recipes, int itemsPrinted, int itemsRequested)
    {
        BlueprintType = blueprintType;
        Recipes = recipes;
        ItemsPrinted = itemsPrinted;
        ItemsRequested = itemsRequested;
    }
}

public sealed class BlueprintLatheGetRecipesEvent : EntityEventArgs
{
    public readonly EntityUid Lathe;

    public Dictionary<ProtoId<BlueprintPrototype>, int[]> UnlockedRecipes = new();

    public BlueprintLatheGetRecipesEvent(EntityUid lathe)
    {
        Lathe = lathe;
    }
}

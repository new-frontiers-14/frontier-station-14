using Content.Shared.Nutrition.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Hydrocraft;

/// <summary>
/// Should be applied to any mob that you want to be able to produce any material with an action and the cost of thirst.
/// TODO: Probably adjust this to utilize organs?
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedHydrocraftSystem)), AutoGenerateComponentState]
public sealed partial class HydrocraftComponent : Component
{
    /// <summary>
    /// The text that pops up whenever sericulture fails for not having enough thirst.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PopupText = "hydrocraft-failure-thirst";

    /// <summary>
    /// What will be produced at the end of the action.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EntityProduced;

    /// <summary>
    /// The entity needed to actually preform sericulture. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// How long will it take to make.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ProductionLength = 3f;

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current thirst of the mob doing sericulture.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThirstCost = 5f;

    /// <summary>
    /// The lowest thirst threshold that this mob can be in before it's allowed to spin silk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ThirstThreshold MinThirstThreshold = ThirstThreshold.Okay;
}

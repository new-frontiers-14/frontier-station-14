using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Skrungler.Components;

[RegisterComponent]
public sealed partial class SkrunglerComponent : Component
{
    /// <summary>
    /// This gets set for each mob it processes.
    /// When it hits 0, there is a chance for the skrungler to either spill blood.
    /// </summary>
    [ViewVariables]
    public float RandomMessTimer = 0f;

    /// <summary>
    /// The interval for <see cref="RandomMessTimer"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan RandomMessInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// This gets set for each mob it processes.
    /// When it hits 0, spit out fuel.
    /// </summary>
    [ViewVariables]
    public float ProcessingTimer = default;

    /// <summary>
    /// Amount of fuel that the mob being processed will yield.
    /// This is calculated from the YieldPerUnitMass.
    /// Also stores non-integer leftovers.
    /// </summary>
    [ViewVariables]
    public float CurrentExpectedYield = 0f;

    /// <summary>
    /// The reagent that will be spilled while processing a mob.
    /// </summary>
    [ViewVariables]
    public string? BloodReagent;

    /// <summary>
    /// How many units of fuel it produces for each unit of mass.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float YieldPerUnitMass = default;

    /// <summary>
    /// The base yield per mass unit when no components are upgraded.
    /// </summary>
    [DataField]
    public float BaseYieldPerUnitMass = 20.0f;

    /// <summary>
    /// Machine part whose rating modifies the yield per mass.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartYieldAmount = "MatterBin";

    /// <summary>
    /// How much the machine part quality affects the yield.
    /// Going up a tier will multiply the yield by this amount.
    /// </summary>
    [DataField]
    public float PartRatingYieldAmountMultiplier = 1.25f;

    /// <summary>
    /// How many seconds to take to insert an entity per unit of its mass.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseInsertionDelay = 0.1f;

    /// <summary>
    /// The time it takes to process a mob, per mass.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ProcessingTimePerUnitMass = default;

    /// <summary>
    /// The base time per mass unit that it takes to process a mob
    /// when no components are upgraded.
    /// </summary>
    [DataField]
    public float BaseProcessingTimePerUnitMass = 0.5f;

    /// <summary>
    /// The machine part that increses the processing speed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartProcessingSpeed = "Manipulator";

    /// <summary>
    /// How much the machine part quality affects the yield.
    /// Going up a tier will multiply the speed by this amount.
    /// </summary>
    [DataField]
    public float PartRatingSpeedMultiplier = 1.35f;
}

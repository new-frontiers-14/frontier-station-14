using Content.Shared.Chemistry.Reagent;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NF.Skrungler.Components;

/// <summary>
/// An entity that can process mobs into fuel, spilling their blood into a puddle around the machine.
/// Great for parties.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SkrunglerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// This gets set for each mob it processes.
    /// When it hits 0, there is a chance for the skrungler to either spill blood.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextMessTime;

    /// <summary>
    /// The interval for <see cref="NextMessTime"/>.
    /// </summary>
    [DataField]
    public TimeSpan MessInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// This gets set for each mob it processes.
    /// When it hits 0, spit out fuel.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FinishProcessingTime;

    /// <summary>
    /// Amount of fuel that the mob being processed will yield.
    /// This is calculated from the YieldPerUnitMass.
    /// Also stores non-integer leftovers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentExpectedYield;

    /// <summary>
    /// The reagent that will be spilled while processing a mob.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? BloodReagent;

    /// <summary>
    /// The output of the mob being processed.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<StackPrototype> OutputStackType;

    /// <summary>
    /// How many units of fuel it produces for each unit of mass.
    /// </summary>
    [DataField]
    public float YieldPerUnitMass;

    /// <summary>
    /// The base yield (in stack count) per mass unit when no components are upgraded.
    /// </summary>
    [DataField]
    public float BaseYieldPerUnitMass = 0.2f;

    /// <summary>
    /// Machine part whose rating modifies the yield per mass.
    /// </summary>
    [DataField]
    public ProtoId<MachinePartPrototype> MachinePartYieldAmount = "MatterBin";

    /// <summary>
    /// How much the machine part quality affects the yield.
    /// Going up a tier will multiply the yield by this amount.
    /// </summary>
    [DataField]
    public float PartRatingYieldAmountMultiplier = 1.25f;

    /// <summary>
    /// How many seconds to take to insert an entity per unit of its mass.
    /// </summary>
    [DataField]
    public float BaseInsertionDelay = 0.1f;

    /// <summary>
    /// The time it takes to process a mob, per mass.
    /// </summary>
    [DataField]
    public TimeSpan ProcessingTimePerUnitMass;

    /// <summary>
    /// The base time per mass unit that it takes to process a mob
    /// when no components are upgraded.
    /// </summary>
    [DataField]
    public TimeSpan BaseProcessingTimePerUnitMass = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The machine part that increases the processing speed.
    /// </summary>
    [DataField]
    public ProtoId<MachinePartPrototype> MachinePartProcessingSpeed = "Manipulator";

    /// <summary>
    /// How much the machine part quality affects the yield.
    /// Going up a tier will multiply the speed by this amount.
    /// </summary>
    [DataField]
    public float PartRatingSpeedMultiplier = 1.35f;

    [DataField]
    public SoundSpecifier SkrungStartSound = new SoundCollectionSpecifier("gib");

    [DataField]
    public SoundSpecifier SkrunglerSound = new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");

    [DataField]
    public SoundSpecifier SkrungFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}

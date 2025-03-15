using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Content.Shared.Fluids;

namespace Content.Shared._NF.Fluids.Components;

/// <summary>
/// A Drain allows an entity to absorb liquid in a disposal goal. Drains can be filled manually (with the Empty verb)
/// or they can absorb puddles of liquid around them when AutoDrain is set to true.
/// When the entity also has a SolutionContainerManager attached with a solution named drainBuffer, this solution
/// gets filled until the drain is full.
/// When the drain is full, it can be unclogged using a plunger (i.e. an entity with a Plunger tag attached).
/// Later this can be refactored into a proper Plunger component if needed.
/// </summary>
[RegisterComponent, Access(typeof(SharedDrainSystem))]
public sealed partial class AdvDrainComponent : Component
{
    public const string SolutionName = "drainBuffer";

    [ValidatePrototypeId<TagPrototype>]
    public const string PlungerTag = "Plunger";

    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    [DataField]
    public float Accumulator = 0f;

    /// <summary>
    /// Does this drain automatically absorb surrouding puddles? Or is it a drain designed to empty
    /// solutions in it manually?
    /// </summary>
    [DataField]
    public bool AutoDrain = true;

    /// <summary>
    /// How many units per second the drain can absorb from the surrounding puddles.
    /// Divided by puddles, so if there are 5 puddles this will take 1/5 from each puddle.
    /// This will stay fixed to 1 second no matter what DrainFrequency is.
    /// </summary>
    [DataField]
    public float UnitsPerSecond = 20f;

    /// <summary>
    /// How many units are ejected from the buffer per second.
    /// </summary>
    [DataField]
    public float UnitsDestroyedPerSecond = 15f;

    /// <summary>
    /// Threshold of volume to begin destroying from the buffer. The effective capacity of the drain.
    /// </summary>
    [DataField]
    public float UnitsDestroyedThreshold = 600f;

    /// <summary>
    /// How many (unobstructed) tiles away the drain will
    /// drain puddles from.
    /// </summary>
    [DataField]
    public float Range = 2.5f;

    /// <summary>
    /// How often in seconds the drain checks for puddles around it.
    /// If the EntityQuery seems a bit unperformant this can be increased.
    /// </summary>
    [DataField]
    public float DrainFrequency = 1f;

    /// <summary>
    /// How many watts does the device need?
    /// </summary>
    [DataField]
    public float Wattage = 15f;

    /// <summary>
    /// How many watts does the device need?
    /// </summary>
    [DataField]
    public bool gridPowered = false;

    [DataField]
    public SoundSpecifier ManualDrainSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
}

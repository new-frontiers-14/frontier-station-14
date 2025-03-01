using System.Numerics;

namespace Content.Server._NF.Traits.Assorted;

/// <summary>
/// This is used for the parkinson trait.
/// </summary>
[RegisterComponent, Access(typeof(ParkinsonTraitSystem))]
public sealed partial class ParkinsonTraitComponent : Component
{
    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField]
    public Vector2 TimeBetweenIncidents { get; private set; } = new(10, 30);

    [DataField]
    public int IncidentLength { get; private set; } = 2;

    [DataField]
    public float IncidentAmplitude { get; private set; } = 40f;

    [DataField]
    public float IncidentFrequency { get; private set; } = 4f;

    public float NextIncidentTime;
}

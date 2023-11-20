using System.Numerics;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This is used for the stinky trait.
/// </summary>
[RegisterComponent, Access(typeof(StinkyTraitSystem))]
public sealed partial class StinkyTraitComponent : Component
{
    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; private set; } = 30, 300;

    /// <summary>
    /// The duration of incidents, (min, max).
    /// </summary>
    [DataField("durationOfIncident", required: true)]
    public Vector2 DurationOfIncident { get; private set; } = 0.1, 0.1;

    public float NextIncidentTime;

    public bool IsActive = true;

    public bool Miasma = false;
}

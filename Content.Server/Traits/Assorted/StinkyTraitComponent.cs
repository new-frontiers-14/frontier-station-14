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
    public Vector2 TimeBetweenIncidents { get; private set; }; // TODO: Fix this to have added numbers here to allow mid game adding.

    /// <summary>
    /// The duration of incidents, (min, max).
    /// </summary>
    [DataField("durationOfIncident", required: true)]
    public Vector2 DurationOfIncident { get; private set; }; // TODO: Fix this to have added numbers here to allow mid game adding.

    public float NextIncidentTime;

    public bool IsActive = true;

    public bool Miasma = false;
}

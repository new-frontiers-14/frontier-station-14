using System.Numerics;
using Content.Shared.Atmos;

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
    [DataField("timeBetweenIncidents")]
    public Vector2 TimeBetweenIncidents { get; private set; } = new(300, 600);

    public float NextIncidentTime;

    public bool IsActive = true;

    /// <summary>
    /// The duration of incidents, (min, max).
    /// </summary>
    [DataField("durationOfIncident")]
    public Vector2 DurationOfIncident { get; private set; } = new(0.1f, 0.1f);

    public bool SpreadGas = false;

    public Gas? SpawnGas { get; set; } = null;
}

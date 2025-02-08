using System.Numerics;

namespace Content.Server._NF.Traits.Assorted;

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
}

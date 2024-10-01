namespace Content.Server._NF.Smuggling.Components;

/// <summary>
///     Denotes a station as one that has dead drops spawned on it.
///     When the map is initialized, anything with PotentialComponent
///     may become a DeadDrop.
///
///     When a dead drop on the station is compromised, another
///     potential dead drop is selected instead.
/// </summary>
[RegisterComponent]
public sealed partial class StationDeadDropComponent : Component
{
    /// <summary>
    ///     Maximum number of dead drops to spawn on the station.
    /// </summary>
    [DataField]
    public int MaxDeadDrops = 3;
}

namespace Content.Server._NF.Smuggling.Components;

/// <summary>
///     Denotes a grid that is brought in via a dead drop.
/// </summary>
[RegisterComponent]
public sealed partial class ContrabandPodGridComponent : Component
{
    /// <summary>
    ///     Maximum number of dead drops to spawn on the station.
    /// </summary>
    [DataField]
    public bool Scanned = false;
}

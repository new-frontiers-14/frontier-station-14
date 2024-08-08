namespace Content.Server._NF.Smuggling.Components;

/// <summary>
///     Stores dead drop information for the entire sector.
///     Frequency of dead drops, and other dead drop mechanics should be driven by this state.
/// </summary>
[RegisterComponent]
public sealed partial class SectorDeadDropComponent : Component
{
    /// <summary>
    ///     Number of reported dead drops.
    /// </summary>
    [ViewVariables]
    public int NumDeadDropsReported = 0;

    /// <summary>
    ///     Maximum number of dead drops sector-wide.
    /// </summary>
    /// <remarks>
    ///     Should this be a CVAR?
    /// </remarks>
    [ViewVariables]
    public int MaxSectorDeadDrops = 10;

    /// <summary>
    ///     Maximum number of summoned syndicate cargo pods available at once.
    /// </summary>
    [ViewVariables]
    public int MaxSimultaneousPods = 3;

    // TODO: add some heat/filtering on how many dead drops there should be at once.
}

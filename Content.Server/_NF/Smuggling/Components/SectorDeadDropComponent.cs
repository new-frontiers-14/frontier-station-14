using Content.Shared.FixedPoint;

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
    [DataField, ViewVariables]
    public int MaxSectorDeadDrops = 10;

    /// <summary>
    ///     Maximum number of summoned syndicate cargo pods available at once.
    /// </summary>
    /// <remarks>
    ///     Should this be a CVAR?
    /// </remarks>
    [DataField, ViewVariables]
    public int MaxSimultaneousPods = 3;

    /// <summary>
    ///     Accumulator for FUC values.  Pays out at a given amount.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 FUCAccumulator = FixedPoint2.Zero;

    /// <summary>
    ///     Minimum FUC for payout.  FUCs should be paid out whenever the accumulator is greater than this value, keeping any remainder.
    /// </summary>
    /// <remarks>
    ///     Should this be a CVAR?
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinFUCPayout = 2;

    // Utility field for windowing reported events.  Having more in an hour results in more precise information.
    [ViewVariables(VVAccess.ReadWrite)]
    public WindowedCounter ReportedEventsThisHour = new(TimeSpan.FromMinutes(60));

    /// <summary>
    ///     Number of reported events required before getting locations with alternatives (e.g. "smuggling either at Tinnia's or Grifty's")
    /// </summary>
    /// <remarks>
    ///     Should this be a CVAR?
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ReportAlternativeThreshold = 1;

    /// <summary>
    ///     Number of reported events required before getting precise locations (e.g. "smuggling at Tinnia's")
    /// </summary>
    /// <remarks>
    ///     Should this be a CVAR?
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ReportPreciseThreshold = 2;

    // Which station do our reports come from?
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid ReportingStation = EntityUid.Invalid;

    // Which grid do our reports come from?
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid ReportingGrid = EntityUid.Invalid;

    // In the case of providing a fake location for alternative notifications, which names can we draw from?
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<EntityUid, string> DeadDropStationNames = new();
}

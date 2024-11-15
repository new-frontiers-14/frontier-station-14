using Content.Shared.Radio; // Frontier
using Content.Shared.Roles; // Frontier
using Robust.Shared.Audio;
using Robust.Shared.Prototypes; // Frontier
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Defines basic data for a station event
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StationEventComponent : Component
{
    public const float WeightVeryLow = 0.0f;
    public const float WeightLow = 5.0f;
    public const float WeightNormal = 10.0f;
    public const float WeightHigh = 15.0f;
    public const float WeightVeryHigh = 20.0f;

    [DataField]
    public float Weight = WeightNormal;

    [DataField]
    public string? StartAnnouncement;

    [DataField]
    public string? WarningAnnouncement; // Frontier

    [DataField]
    public string? EndAnnouncement;

    [DataField]
    public Color StartAnnouncementColor = Color.Gold;

    [DataField]
    public Color WarningAnnouncementColor = Color.Gold; // Frontier

    [DataField]
    public Color EndAnnouncementColor = Color.Gold;

    [DataField]
    public SoundSpecifier? StartAudio;

    [DataField]
    public SoundSpecifier? WarningAudio; // Frontier

    [DataField]
    public SoundSpecifier? EndAudio;

    /// <summary>
    /// Frontier: Radio channels on which announcements are transmitted
    /// </summary>
    [DataField]
    public string? StartRadioAnnouncement; // Frontier

    [DataField]
    public string? WarningRadioAnnouncement; // Frontier

    [DataField]
    public string? EndRadioAnnouncement; // Frontier

    [DataField]
    public ProtoId<RadioChannelPrototype> StartRadioAnnouncementChannel = "Supply"; // Frontier

    [DataField]
    public ProtoId<RadioChannelPrototype> WarningRadioAnnouncementChannel = "Supply"; // Frontier

    [DataField]
    public ProtoId<RadioChannelPrototype> EndRadioAnnouncementChannel = "Supply"; // Frontier

    /// <summary>
    ///     In minutes, when is the first round time this event can start
    /// </summary>
    [DataField]
    public int EarliestStart = 5;

    /// <summary>
    ///     In minutes, the amount of time before the same event can occur again
    /// </summary>
    [DataField]
    public int ReoccurrenceDelay = 30;

    /// <summary>
    ///     How long the event lasts.
    /// </summary>
    [DataField]
    public TimeSpan? Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The max amount of time the event lasts.
    /// </summary>
    [DataField]
    public TimeSpan? MaxDuration;

    /// <summary>
    ///     How many players need to be present on station for the event to run
    /// </summary>
    /// <remarks>
    ///     To avoid running deadly events with low-pop
    /// </remarks>
    [DataField]
    public int MinimumPlayers;

    /// <summary>
    ///     Frontier: How many players need to be present on station for the event to not run, to avoid running safe events with high-pop
    /// </summary>
    [DataField]
    public int MaximumPlayers = 999;

    /// <summary>
    ///     How many times this even can occur in a single round
    /// </summary>
    [DataField]
    public int? MaxOccurrences;

    /// <summary>
    /// When the station event ends.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? EndTime;

    /// <summary>
    /// If false, the event won't trigger during ongoing evacuation.
    /// </summary>
    [DataField]
    public bool OccursDuringRoundEnd = true;

    /// <summary>
    ///     Frontier: Require active job to run the event.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> RequiredJobs = new();

    /// <summary>
    ///     Frontier: Warning timer.
    /// </summary>
    [DataField]
    public int WarningDurationLeft = 300; // 5 minutes

    /// <summary>
    ///     Frontier: True if the warning has already been sent off.
    /// </summary>
    [DataField]
    public bool WarningAnnounced;
}

using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Magnet pickup behavior types
/// </summary>
public enum MagnetPickupType
{
    /// <summary>
    /// Regular storage magnet - picks up items into storage containers
    /// </summary>
    Storage,

    /// <summary>
    /// Material storage magnet - picks up material entities into material storage
    /// </summary>
    MaterialStorage,

    /// <summary>
    /// Material reclaimer magnet - picks up items to process in material reclaimer
    /// </summary>
    MaterialReclaimer
}

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NFMagnetPickupComponent : Component
{
    // Default constants
    public const int DefaultVerbPriority = 3;

    [AutoPausedField, ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan NextScan { get; set; } = TimeSpan.Zero;

    // Timing constants
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public static TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public static TimeSpan FastScanDelay = TimeSpan.FromSeconds(0.5);

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public static TimeSpan SlowScanDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Type of magnet pickup behavior
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
    public MagnetPickupType PickupType { get; set; } = MagnetPickupType.Storage;

    /// <summary>
    /// Is the magnet currently enabled?
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
    public bool MagnetEnabled { get; set; } = false;

    // Processing limits
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public const int MaxEntitiesPerScan = 20;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public const int MaxPickupsPerScan = 5;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float Range { get; set; } = 1f;

    /// <summary>
    /// Priority for the toggle magnet verb.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int MagnetTogglePriority { get; set; } = DefaultVerbPriority;

    /// <summary>
    /// Whether auto-disable is enabled. When true, magnet will automatically disable after AutoDisableTime without successful pickups.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
    public bool AutoDisableEnabled { get; set; } = false;

    /// <summary>
    /// Time to wait before auto-disabling the magnet if no successful pickups occur.
    /// </summary>
    [AutoPausedField, ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan AutoDisableTime { get; set; } = TimeSpan.FromSeconds(600);

    /// <summary>
    /// Time when the last successful pickup occurred.
    /// </summary>
    [AutoPausedField, ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan LastSuccessfulPickup { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// When true, the magnet runs indefinitely without auto-disable or manual toggle capability.
    /// Useful for permanent magnet installations or special magnet types.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
    public bool AlwaysOn { get; set; } = false;

    /// <summary>
    /// Time remaining until auto-disable
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilAutoDisable
    {
        get
        {
            if (!AutoDisableEnabled || LastSuccessfulPickup == TimeSpan.Zero || AlwaysOn)
                return TimeSpan.Zero;

            var elapsed = IoCManager.Resolve<IGameTiming>().CurTime - LastSuccessfulPickup;
            var remaining = AutoDisableTime - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    // Texture paths
    [DataField]
    public const string PowerToggleIconPath = "/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png";

    // Localization keys
    [DataField]
    public const string VerbToggleText = "magnet-pickup-toggle-verb";

    [DataField]
    public const string ExamineText = "magnet-pickup-examine";

    [DataField]
    public const string ExamineStateEnabled = "magnet-pickup-state-enabled";

    [DataField]
    public const string ExamineStateDisabled = "magnet-pickup-state-disabled";
}

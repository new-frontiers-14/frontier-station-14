using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Magnet pickup behavior types
/// </summary>
public enum MagnetPickupType : byte
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
/// Unified magnet pickup component that handles all magnet types.
/// Provides configurable magnetic item collection with auto-disable and toggle functionality.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NFMagnetPickupComponent : Component
{
    #region Constants

    /// <summary>
    /// Default priority for magnet toggle verbs
    /// </summary>
    public const int DefaultVerbPriority = 3;

    #endregion

    #region Performance Configuration

    /// <summary>
    /// Maximum entities to process per scan cycle (performance limit)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int MaxEntitiesPerScan { get; set; } = 20;

    /// <summary>
    /// Maximum successful pickups per scan cycle (performance limit)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int MaxPickupsPerScan { get; set; } = 10;

    #endregion

    #region Timing Configuration

    /// <summary>
    /// Standard scan interval when magnet is active
    /// </summary>
    public static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Fast scan interval after successful pickups
    /// </summary>
    public static readonly TimeSpan FastScanDelay = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Slow scan interval when no targets found
    /// </summary>
    public static readonly TimeSpan SlowScanDelay = TimeSpan.FromSeconds(2);

    #endregion

    #region Core Properties

    /// <summary>
    /// Type of magnet pickup behavior
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public MagnetPickupType PickupType { get; set; } = MagnetPickupType.Storage;

    /// <summary>
    /// Whether the magnet is currently enabled
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool MagnetEnabled { get; set; } = false;

    /// <summary>
    /// Pickup range in meters
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Range { get; set; } = 1f;

    #endregion

    #region Auto-Disable Functionality

    /// <summary>
    /// Whether auto-disable is enabled. When true, magnet automatically disables after AutoDisableTime without successful pickups.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool AutoDisableEnabled { get; set; } = false;

    /// <summary>
    /// Time to wait before auto-disabling the magnet if no successful pickups occur
    /// </summary>
    [AutoPausedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public TimeSpan AutoDisableTime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// When true, the magnet runs indefinitely without auto-disable or manual toggle capability.
    /// Useful for permanent magnet installations.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool AlwaysOn { get; set; } = false;

    #endregion

    #region Internal State

    /// <summary>
    /// Next scheduled scan time
    /// </summary>
    [AutoPausedField]
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public TimeSpan NextScan { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Time when the last successful pickup occurred
    /// </summary>
    [AutoPausedField]
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public TimeSpan LastSuccessfulPickup { get; set; } = TimeSpan.Zero;

    #endregion

    #region UI Configuration

    /// <summary>
    /// Priority for the toggle magnet verb
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int MagnetTogglePriority { get; set; } = DefaultVerbPriority;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Time remaining until auto-disable (read-only for UI display)
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

    #endregion

    #region Localization Keys

    /// <summary>
    /// Localization key for toggle verb text
    /// </summary>
    public const string VerbToggleText = "magnet-pickup-component-toggle-verb";

    /// <summary>
    /// Localization key for main examine text
    /// </summary>
    public const string ExamineText = "magnet-pickup-component-on-examine-main";

    /// <summary>
    /// Localization key for magnet on state
    /// </summary>
    public const string ExamineTextOn = "magnet-pickup-component-magnet-on";

    /// <summary>
    /// Localization key for magnet off state
    /// </summary>
    public const string ExamineTextOff = "magnet-pickup-component-magnet-off";

    /// <summary>
    /// Localization key for always-on magnet state
    /// </summary>
    public const string ExamineTextAlwaysOn = "magnet-pickup-component-magnet-always-on";

    /// <summary>
    /// Localization key for always-off magnet state
    /// </summary>
    public const string ExamineTextAlwaysOff = "magnet-pickup-component-magnet-always-off";

    #endregion

    #region Icon Paths

    /// <summary>
    /// Icon path for power toggle verb
    /// </summary>
    public const string PowerToggleIconPath = "/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png";

    #endregion
}

using Robust.Shared.GameStates;

namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NFMagnetPickupComponent : Component, IBaseMagnetPickupComponent
{
    [AutoPausedField, ViewVariables, DataField]
    public TimeSpan NextScan { get; set; } = TimeSpan.Zero;

    // Timing constants
    [ViewVariables, DataField]
    public static TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    [ViewVariables, DataField]
    public static TimeSpan FastScanDelay = TimeSpan.FromSeconds(0.5);

    [ViewVariables, DataField]
    public static TimeSpan SlowScanDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Is the magnet currently enabled?
    /// </summary>
    [AutoNetworkedField, ViewVariables, DataField]
    public bool MagnetEnabled { get; set; } = true;

    /// <summary>
    /// Can the magnet be toggled by the user?
    /// </summary>
    [ViewVariables, DataField]
    public bool MagnetCanBeEnabled { get; set; } = true;

    // Processing limits
    [ViewVariables, DataField]
    public const int MaxEntitiesPerScan = 20;

    [ViewVariables, DataField]
    public const int MaxPickupsPerScan = 5;

    [ViewVariables, DataField]
    public float Range { get; set; } = 1f;

    /// <summary>
    /// Priority for the toggle magnet verb.
    /// </summary>
    [ViewVariables, DataField]
    public int MagnetTogglePriority { get; set; } = DefaultVerbPriority;

    // Default constants
    public const int DefaultVerbPriority = 3;

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

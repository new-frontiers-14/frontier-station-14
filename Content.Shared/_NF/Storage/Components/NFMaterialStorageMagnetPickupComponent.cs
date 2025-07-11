namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent]
public sealed partial class NFMaterialStorageMagnetPickupComponent : Component, IBaseMagnetPickupComponent
{
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan NextScan { get; set; } = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float Range { get; set; } = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool MagnetEnabled { get; set; } = false;

    /// <summary>
    /// Priority for the toggle magnet verb.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int MagnetTogglePriority { get; set; } = NFMagnetPickupComponent.DefaultVerbPriority;

    /// <summary>
    /// Whether auto-disable is enabled. When true, magnet will automatically disable after AutoDisableTime without successful pickups.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool AutoDisableEnabled { get; set; } = false;

    /// <summary>
    /// Time to wait before auto-disabling the magnet if no successful pickups occur.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan AutoDisableTime { get; set; } = TimeSpan.FromSeconds(600); // 10 minutes default

    /// <summary>
    /// Time when the last successful pickup occurred.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan LastSuccessfulPickup { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// When true, the magnet runs indefinitely without auto-disable or manual toggle capability.
    /// Useful for permanent magnet installations or special magnet types.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool AlwaysOn { get; set; } = false;

    // Processing limits (shared constants)
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public const int MaxEntitiesPerScan = 20;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public const int MaxPickupsPerScan = 5;
}

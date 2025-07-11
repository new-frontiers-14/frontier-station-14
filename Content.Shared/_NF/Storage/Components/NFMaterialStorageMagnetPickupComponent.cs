using Content.Shared._NF.Storage.Components;

namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent]
public sealed partial class NFMaterialStorageMagnetPickupComponent : Component, IBaseMagnetPickupComponent
{
    [ViewVariables, DataField]
    public TimeSpan NextScan { get; set; } = TimeSpan.Zero;

    [ViewVariables, DataField]
    public float Range { get; set; } = 1f;

    [ViewVariables, DataField]
    public bool MagnetEnabled { get; set; } = false;

    /// <summary>
    /// Can the magnet be toggled by the user?
    /// </summary>
    [ViewVariables, DataField]
    public bool MagnetCanBeEnabled { get; set; } = true;

    /// <summary>
    /// Priority for the toggle magnet verb.
    /// </summary>
    [ViewVariables, DataField]
    public int MagnetTogglePriority { get; set; } = NFMagnetPickupComponent.DefaultVerbPriority;

    // Processing limits (shared constants)
    [ViewVariables, DataField]
    public const int MaxEntitiesPerScan = 20;

    [ViewVariables, DataField]
    public const int MaxPickupsPerScan = 5;
}

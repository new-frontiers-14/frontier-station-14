using Content.Shared._NF.Storage.Components;

namespace Content.Shared._NF.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent]
public sealed partial class NFMaterialReclaimerMagnetPickupComponent : Component, IBaseMagnetPickupComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    public TimeSpan NextScan { get; set; } = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range { get; set; } = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("magnetEnabled")]
    public bool MagnetEnabled { get; set; } = false;
}

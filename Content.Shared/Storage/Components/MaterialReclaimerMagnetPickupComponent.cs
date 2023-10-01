using Content.Shared.Tag;
using Content.Shared.Whitelist;

namespace Content.Server.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent]
public sealed partial class MaterialReclaimerMagnetPickupComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    public TimeSpan NextScan = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [ViewVariables(VVAccess.ReadWrite), DataField("blacklist")]
    public EntityWhitelist? Blacklist;
}

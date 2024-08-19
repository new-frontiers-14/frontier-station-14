using Content.Shared.Cargo;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.DeltaV.Cargo.Components;

/// <summary>
/// Tracks all mail statistics for mail activity in the sector.
/// </summary>
[RegisterComponent, Access(typeof(SharedCargoSystem))]
public sealed partial class SectorLogisticStatsComponent : Component // Frontier: Station->Sector
{
    [DataField]
    public MailStats Metrics { get; set; }
}
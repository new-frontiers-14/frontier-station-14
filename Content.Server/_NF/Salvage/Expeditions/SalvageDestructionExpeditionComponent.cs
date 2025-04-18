using Content.Server.Salvage;
using Content.Shared.Salvage;

namespace Content.Server._NF.Salvage.Expeditions.Structure;

/// <summary>
/// Tracks expedition data for <see cref="SalvageMissionType.Destruction"/>
/// </summary>
[RegisterComponent, Access(typeof(SalvageSystem), typeof(SpawnSalvageMissionJob))]
public sealed partial class SalvageDestructionExpeditionComponent : Component
{
    /// <summary>
    /// List of structures that need to be destroyed for the mission to be complete.
    /// </summary>
    [DataField]
    public List<EntityUid> Structures = new();
}

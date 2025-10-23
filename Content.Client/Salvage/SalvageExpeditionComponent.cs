using Content.Shared.Salvage.Expeditions;

namespace Content.Client.Salvage;

[RegisterComponent]
public sealed partial class SalvageExpeditionComponent : SharedSalvageExpeditionComponent
{
    // Frontier: add audio stream
    [DataField]
    public EntityUid? Stream;
    // End Frontier
}

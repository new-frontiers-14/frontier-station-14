using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Salvage.Expeditions;

[NetworkedComponent]
public abstract partial class SharedSalvageExpeditionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("stage")]
    public ExpeditionStage Stage = ExpeditionStage.Added;

    // Frontier: add end of expedition song
    [DataField]
    public ResolvedSoundSpecifier SelectedSong;
    // End Frontier: add end of expedition song
}

[Serializable, NetSerializable]
public sealed class SalvageExpeditionComponentState : ComponentState
{
    public ExpeditionStage Stage;

    // Frontier: add sound
    public ResolvedSoundSpecifier? SelectedSong;
}

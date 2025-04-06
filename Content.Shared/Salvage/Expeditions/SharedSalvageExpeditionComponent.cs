using Robust.Shared.Audio; // Frontier
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Salvage.Expeditions;

[NetworkedComponent]
public abstract partial class SharedSalvageExpeditionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("stage")]
    public ExpeditionStage Stage = ExpeditionStage.Added;

    // Frontier: add end of expedition song
    /// <summary>
    /// Song selected on MapInit so we can predict the audio countdown properly.
    /// </summary>
    [DataField]
    public ResolvedSoundSpecifier SelectedSong;
    // End Frontier: add end of expedition song
}

[Serializable, NetSerializable]
public sealed class SalvageExpeditionComponentState : ComponentState
{
    public ExpeditionStage Stage;
    public ResolvedSoundSpecifier? SelectedSong; // Frontier: add end of expedition song
}

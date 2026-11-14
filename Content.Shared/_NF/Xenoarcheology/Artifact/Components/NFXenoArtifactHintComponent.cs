using Robust.Shared.GameStates;

namespace Content.Shared._NF.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Provides a vague hint, such as "environmental disruption", on artifact nodes
/// that have not yet been unlocked.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NFXenoArtifactHintComponent : Component
{
    [DataField]
    public LocId? Hint = default;
}

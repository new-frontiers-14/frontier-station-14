using Robust.Shared.Prototypes;

namespace Content.Server._NF.Transfer.Components;
/// <summary>
/// Its not fancy but it works for an in-between animations used on
/// hatching animation of the baby dragon
/// </summary>

[RegisterComponent]
public sealed partial class TransferMindOnDespawnComponent : Component
{
    /// <summary>
    /// The entity prototype to move the mind to after the animation.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EntityPrototype = default!;
}

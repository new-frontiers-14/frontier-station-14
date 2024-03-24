using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._NF.Clothing.Components;

/// <summary>
///   Indicates that the clothing entity emits sound when it moves.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitsSoundOnMoveComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier SoundCollection = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("requiresGravity"), AutoNetworkedField]
    public bool RequiresGravity = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates LastPosition = EntityCoordinates.Invalid;

    /// <summary>
    ///   The distance moved since the played sound.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float SoundDistance = 0f;

    /// <summary>
    ///   Whether this item is equipped in a inventory item slot.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSlotValid = true;
}

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._NF.Clothing.Components;

/// <summary>
///   Indicates that the clothing entity emits sound when its wearer moves.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SoundEmittingClothingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier SoundCollection = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("requiresGravity"), AutoNetworkedField]
    public bool RequiresGravity = true;
}

/// <summary>
///   Added internally to the entity that wears a clothing item with <see cref="SoundEmittingClothingComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SoundEmittingEntityComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public SoundSpecifier SoundCollection = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public bool RequiresGravity = true;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public EntityCoordinates LastPosition;

    /// <summary>
    ///   The distance moved since the played sound.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public float SoundDistance = 0f;
}

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes; // imp edit
using Content.Shared.Alert; // imp edit

namespace Content.Shared._DV.Waddle;

/// <summary>
/// Adds <see cref="WaddleAnimationComponent"/> to the user when worn.
/// All non-null fields get set on the above component.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(WaddleClothingSystem))]
[AutoGenerateComponentState]
public sealed partial class WaddleWhenWornComponent : Component
{
    /// <summary>
    /// <see cref="WaddleAnimationComponent.HopIntensity"/>
    /// </summary>
    [DataField]
    public Vector2? HopIntensity;

    /// <summary>
    /// <see cref="WaddleAnimationComponent.TumbleIntensity"/>
    /// </summary>
    [DataField]
    public float? TumbleIntensity;

    /// <summary>
    /// <see cref="WaddleAnimationComponent.AnimationLength"/>
    /// </summary>
    [DataField]
    public TimeSpan? AnimationLength;

    /// <summary>
    /// <see cref="WaddleAnimationComponent.RunAnimationLengthMultiplier"/>
    /// </summary>
    [DataField]
    public float? RunAnimationLengthMultiplier;

    /// <summary>
    /// Used to prevent double-removing WaddleAnimation, e.g. if you have a species that waddles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AddedWaddle;

    /// <summary>
    /// Alert displayed while waddling is on. Imp addition
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype>? WaddlingAlert;
}

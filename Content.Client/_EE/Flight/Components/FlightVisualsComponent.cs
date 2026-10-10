using Robust.Client.Graphics;

namespace Content.Client._EE.Flight.Components;

[RegisterComponent]
public sealed partial class FlightVisualsComponent : Component
{
    /// <summary>
    ///     The speed of the shader animation.
    /// </summary>
    [DataField]
    public float Speed;
    /// <summary>
    ///     How far it goes in any direction.
    /// </summary>
    [DataField]
    public float Multiplier;

    /// <summary>
    ///     How much the limbs (if there are any) rotate.
    /// </summary>
    [DataField]
    public float Offset;

    /// <summary>
    ///     Are we animating layers or the entire sprite?
    /// </summary>
    public bool AnimateLayer = false;
    public int? TargetLayer;

    [DataField]
    public string AnimationKey = "default";

    [ViewVariables(VVAccess.ReadWrite)]
    public ShaderInstance Shader = default!;
}
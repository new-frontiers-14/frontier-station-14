using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._EE.Flight;

/// <summary>
///     Adds an action that allows the user to become temporarily
///     weightless at the cost of stamina and hand usage.
/// </summary>
[RegisterComponent, NetworkedComponent(), AutoGenerateComponentState]
public sealed partial class FlightComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ToggleAction = "ActionToggleFlight";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    ///     Is the user flying right now?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCurrentlyFlying;

    /// <summary>
    ///     Stamina drain per second when flying
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StaminaDrainRate = 10.0f;

    /// <summary>
    ///     DeltaV - Stamina cost when taking off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float InitialStaminaCost = 0f;

    /// <summary>
    ///     DoAfter delay until the user becomes weightless.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ActivationDelay = 0.5f;

    /// <summary>
    ///     Speed modifier while in flight
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 2.0f;

    /// <summary>
    ///     DeltaV - Friction modifier while in flight. Should be less than one so 
    ///     they have less control while flying. Also applies to friction with no inputs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrictionModifier = 1f;

    /// <summary>
    ///     DeltaV - Acceleration modifer while in flight.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AccelerationModifer = 1.5f;

    /// <summary>
    ///     Path to a sound specifier or collection for the noises made during flight
    /// </summary>
    [DataField]
    public SoundSpecifier FlapSound = new SoundCollectionSpecifier("WingFlaps");

    /// <summary>
    ///     Is the flight animated?
    /// </summary>
    [DataField]
    public bool IsAnimated = true;

    /// <summary>
    ///     Does the animation animate a layer?.
    /// </summary>
    [DataField]
    public bool IsLayerAnimated;

    /// <summary>
    ///     Which RSI layer path does this animate?
    /// </summary>
    [DataField]
    public string? Layer;

    /// <summary>
    ///     Whats the speed of the shader?
    /// </summary>
    [DataField]
    public float ShaderSpeed = 6.0f;

    /// <summary>
    ///     How much are the values in the shader's calculations multiplied by?
    /// </summary>
    [DataField]
    public float ShaderMultiplier = 0.02f;

    /// <summary>
    ///     What is the offset on the shader?
    /// </summary>
    [DataField]
    public float ShaderOffset = 0.25f;

    /// <summary>
    ///     What animation does the flight use?
    /// </summary>

    [DataField]
    public string AnimationKey = "default";

    /// <summary>
    ///     Time between sounds being played
    /// </summary>
    [DataField]
    public float FlapInterval = 1.0f;

    public float TimeUntilFlap;
}
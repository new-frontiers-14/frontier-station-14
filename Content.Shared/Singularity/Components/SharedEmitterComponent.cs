using System.Threading;
using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitterComponent : Component
{
    public CancellationTokenSource? TimerCancel;

    // whether the power switch is in "on"
    [ViewVariables] public bool IsOn;
    // Whether the power switch is on AND the machine has enough power (so is actively firing)
    [ViewVariables] public bool IsPowered;

    /// <summary>
    /// counts the number of consecutive shots fired.
    /// </summary>
    [ViewVariables]
    public int FireShotCounter;

    /// <summary>
    /// The entity that is spawned when the emitter fires.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId BoltType = "NFEmitterBolt"; // Frontier: use NF prefix

    [DataField]
    public List<EntProtoId> SelectableTypes = new();

    /// <summary>
    /// The current amount of power being used.
    /// </summary>
    [DataField]
    public int PowerUseActive = 1500; // Frontier 600<1500

    /// <summary>
    /// The amount of shots that are fired in a single "burst"
    /// </summary>
    [DataField]
    public int FireBurstSize = 3;

    /// <summary>
    /// The time between each shot during a burst.
    /// </summary>
    [DataField]
    public TimeSpan FireInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The current minimum delay between bursts.
    /// </summary>
    [DataField]
    public TimeSpan FireBurstDelayMin = TimeSpan.FromSeconds(4);

    /// <summary>
    /// The current maximum delay between bursts.
    /// </summary>
    [DataField]
    public TimeSpan FireBurstDelayMax = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The visual state that is set when the emitter is turned on
    /// </summary>
    [DataField]
    public string? OnState = "beam";

    /// <summary>
    /// The visual state that is set when the emitter doesn't have enough power.
    /// </summary>
    [DataField]
    public string? UnderpoweredState = "underpowered";

    /// <summary>
    /// Signal port that turns on the emitter.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// Signal port that turns off the emitter.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    /// <summary>
    /// Signal port that toggles the emitter on or off.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// Map of signal ports to entity prototype IDs of the entity that will be fired.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SinkPortPrototype>, EntProtoId> SetTypePorts = new();

    // Frontier: machine part upgrades
    /// <summary>
    /// The multiplier for the base delay between shot bursts as well as
    /// the fire interval
    /// </summary>
    [DataField]
    public float FireRateMultiplier = 0.8f;

    /// <summary>
    /// The machine part that affects burst delay.
    /// </summary>
    [DataField]
    public string MachinePartFireRate = "Capacitor";

    /// <summary>
    /// The base amount of time between each shot during a burst.
    /// </summary>
    [DataField]
    public TimeSpan BaseFireInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The base minimum delay between shot bursts.
    /// Used for machine part rating calculations.
    /// </summary>
    [DataField]
    public TimeSpan BaseFireBurstDelayMin = TimeSpan.FromSeconds(4);

    /// <summary>
    /// The base maximum delay between shot bursts.
    /// Used for machine part rating calculations.
    /// </summary>
    [DataField]
    public TimeSpan BaseFireBurstDelayMax = TimeSpan.FromSeconds(10);
    // End Frontier
}

[NetSerializable, Serializable]
public enum EmitterVisuals : byte
{
    VisualState
}

[Serializable, NetSerializable]
public enum EmitterVisualLayers : byte
{
    Lights
}

[NetSerializable, Serializable]
public enum EmitterVisualState
{
    On,
    Underpowered,
    Off
}

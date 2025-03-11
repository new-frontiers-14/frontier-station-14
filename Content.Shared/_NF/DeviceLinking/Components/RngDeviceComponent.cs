using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared._NF.DeviceLinking.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.DeviceLinking.Components;

/// <summary>
/// Last state of a signal port, used to not spam invoking ports.
/// </summary>
public enum SignalState : byte
{
    Momentary, // Instantaneous pulse high, compatibility behavior
    Low,
    High
}

/// <summary>
/// Frontier: A random number generator device that triggers a random output port when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRngDeviceSystem))]
[AutoGenerateComponentState]
public sealed partial class RngDeviceComponent : Component
{
    [DataField("inputPort")]
    public ProtoId<SinkPortPrototype> InputPort = "Trigger";

    [DataField("outputPorts")]
    [AutoNetworkedField]
    public Dictionary<int, ProtoId<SourcePortPrototype>> OutputPorts = [];

    private static readonly int[] ValidOutputCounts = { 2, 4, 6, 8, 10, 12, 20 };

    /// <summary>
    /// Number of output ports.
    /// </summary>
    [DataField("outputs")]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Outputs = 6;

    /// <summary>
    /// Initial state
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SignalState State = SignalState.Low;

    /// <summary>
    /// Whether the device is muted
    /// </summary>
    [DataField("muted")]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Muted;

    /// <summary>
    /// Target number for percentile dice (1-100). Only used when Outputs = 2.
    /// </summary>
    [DataField("targetNumber")]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int TargetNumber = 50;

    /// <summary>
    /// When enabled, sends High signal to selected port and Low signals to others.
    /// </summary>
    [DataField("edgeMode")]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EdgeMode;

    /// <summary>
    /// The last value rolled (1-100 for percentile, 1-N for other dice).
    /// </summary>
    [DataField("lastRoll")]
    [AutoNetworkedField]
    [ViewVariables]
    public int LastRoll;

    /// <summary>
    /// The last output port that was triggered (1-based).
    /// </summary>
    [DataField("lastOutputPort")]
    [AutoNetworkedField]
    [ViewVariables]
    public int LastOutputPort;

    /// <summary>
    /// Cached state prefix for visual updates
    /// </summary>
    [ViewVariables]
    public string StatePrefix = string.Empty;
}

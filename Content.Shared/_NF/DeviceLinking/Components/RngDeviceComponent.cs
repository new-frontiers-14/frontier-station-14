using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared._NF.DeviceLinking.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.DeviceLinking.Components;

// Last state of a signal port, used to not spam invoking ports.
public enum SignalState : byte
{
    Momentary, // Instantaneous pulse high, compatibility behavior
    Low,
    High
}

// Frontier: A random number generator device that triggers a random output port when triggered.
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRngDeviceSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class RngDeviceComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Trigger";

    [DataField]
    public Dictionary<int, ProtoId<SourcePortPrototype>> OutputPorts = [];

    private static readonly int[] ValidOutputCounts = { 2, 4, 6, 8, 10, 12, 20 };

    // Number of output ports.
    [DataField]
    public int Outputs = 6;

    // Initial state
    [DataField, AutoNetworkedField]
    public SignalState State = SignalState.Low;

    // Whether the device is muted
    [DataField, AutoNetworkedField]
    public bool Muted;

    // Sound to play when the device is rolled
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("Dice");

    // Target number for percentile dice (1-100). Only used when Outputs = 2.
    [DataField, AutoNetworkedField]
    public int TargetNumber = 50;

    // When enabled, sends High signal to selected port and Low signals to others.
    [DataField, AutoNetworkedField]
    public bool EdgeMode;

    // The last value rolled (1-100 for percentile, 1-N for other dice).
    [DataField, AutoNetworkedField]
    public int LastRoll;

    // The last output port that was triggered
    [DataField, AutoNetworkedField]
    public int LastOutputPort;

    // The state prefix for visual updates (e.g. "percentile", "d6", etc.)
    [DataField, AutoNetworkedField]
    public string StatePrefix = "";
}

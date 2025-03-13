using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared._NF.DeviceLinking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server._NF.DeviceLinking.Components;

// Server-side component for RNG device functionality
[RegisterComponent]
public sealed partial class RngDeviceComponent : Component
{
    // The input port that triggers the RNG roll.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("inputPort")]
    public ProtoId<SinkPortPrototype> InputPort { get; set; } = "Trigger";

    // Sound to play when the device is rolled.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sound")]
    public SoundSpecifier Sound { get; set; } = new SoundCollectionSpecifier("Dice");

    // Number of output ports this device has.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("outputs"), AutoNetworkedField]
    public int Outputs { get; set; } = 6;

    // Current signal state of the device
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("state"), AutoNetworkedField]
    public SignalState State { get; set; } = SignalState.Low;

    // Whether the device is muted
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("muted"), AutoNetworkedField]
    public bool Muted { get; set; }

    // Target number for percentile dice (1-100). Only used when Outputs = 2.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("targetNumber"), AutoNetworkedField]
    public int TargetNumber { get; set; } = 50;

    // When enabled, sends High signal to selected port and Low signals to others
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("edgeMode"), AutoNetworkedField]
    public bool EdgeMode { get; set; }

    // The last value rolled (1-100 for percentile, 1-N for other dice)
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lastRoll"), AutoNetworkedField]
    public int LastRoll { get; set; }

    // The last output port that was triggered
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lastOutputPort"), AutoNetworkedField]
    public int LastOutputPort { get; set; }

    // The state prefix for visual updates (e.g. "percentile", "d6", etc.)
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("statePrefix"), AutoNetworkedField]
    public string StatePrefix { get; set; } = "";
}

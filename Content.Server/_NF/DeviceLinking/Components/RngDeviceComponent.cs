using Content.Server.DeviceLinking.Systems;
using Content.Shared._NF.DeviceLinking;
using Robust.Shared.Prototypes;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Frontier: A random number generator device that triggers a random output port when triggered.
/// </summary>
[RegisterComponent, Access(typeof(RngDeviceSystem))]
public sealed partial class RngDeviceComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField("inputPort")]
    public string InputPort = "RngInput";

    /// <summary>
    /// Output port 1 name
    /// </summary>
    [DataField("output1Port")]
    public string Output1Port = "RngOutput1";

    /// <summary>
    /// Output port 2 name
    /// </summary>
    [DataField("output2Port")]
    public string Output2Port = "RngOutput2";

    /// <summary>
    /// Output port 3 name
    /// </summary>
    [DataField("output3Port")]
    public string Output3Port = "RngOutput3";

    /// <summary>
    /// Output port 4 name
    /// </summary>
    [DataField("output4Port")]
    public string Output4Port = "RngOutput4";

    /// <summary>
    /// Output port 5 name
    /// </summary>
    [DataField("output5Port")]
    public string Output5Port = "RngOutput5";

    /// <summary>
    /// Output port 6 name
    /// </summary>
    [DataField("output6Port")]
    public string Output6Port = "RngOutput6";

    /// <summary>
    /// Number of output ports.
    /// </summary>
    [DataField("outputs")]
    public int Outputs = 6;

    /// <summary>
    /// Initial state
    /// </summary>
    [DataField]
    public SignalState State = SignalState.Low;

    [DataField("muted")]
    public bool Muted;

    /// <summary>
    /// Target number for percentile dice (1-100). Only used when Outputs = 2.
    /// </summary>
    [DataField("targetNumber")]
    public int TargetNumber = 50;

    /// <summary>
    /// When enabled, sends High signal to selected port and Low signals to others.
    /// </summary>
    [DataField("edgeMode")]
    public bool EdgeMode;

    /// <summary>
    /// The last value rolled (1-100 for percentile, 1-N for other dice).
    /// </summary>
    [DataField("lastRoll")]
    public int LastRoll;

    /// <summary>
    /// The last output port that was triggered (1-based).
    /// </summary>
    [DataField("lastOutputPort")]
    public int LastOutputPort;

    public BoundUserInterface? UserInterface;
}

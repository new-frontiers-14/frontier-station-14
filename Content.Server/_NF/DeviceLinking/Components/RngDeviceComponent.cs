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
    /// The input port that triggers the RNG device
    /// </summary>
    [DataField("inputPort")]
    public string InputPort = "Trigger";

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
    /// Output port 7 name
    /// </summary>
    [DataField("output7Port")]
    public string Output7Port = "RngOutput7";

    /// <summary>
    /// Output port 8 name
    /// </summary>
    [DataField("output8Port")]
    public string Output8Port = "RngOutput8";

    /// <summary>
    /// Output port 9 name
    /// </summary>
    [DataField("output9Port")]
    public string Output9Port = "RngOutput9";

    /// <summary>
    /// Output port 10 name
    /// </summary>
    [DataField("output10Port")]
    public string Output10Port = "RngOutput10";

    /// <summary>
    /// Output port 11 name
    /// </summary>
    [DataField("output11Port")]
    public string Output11Port = "RngOutput11";

    /// <summary>
    /// Output port 12 name
    /// </summary>
    [DataField("output12Port")]
    public string Output12Port = "RngOutput12";

    /// <summary>
    /// Output port 13 name
    /// </summary>
    [DataField("output13Port")]
    public string Output13Port = "RngOutput13";

    /// <summary>
    /// Output port 14 name
    /// </summary>
    [DataField("output14Port")]
    public string Output14Port = "RngOutput14";

    /// <summary>
    /// Output port 15 name
    /// </summary>
    [DataField("output15Port")]
    public string Output15Port = "RngOutput15";

    /// <summary>
    /// Output port 16 name
    /// </summary>
    [DataField("output16Port")]
    public string Output16Port = "RngOutput16";

    /// <summary>
    /// Output port 17 name
    /// </summary>
    [DataField("output17Port")]
    public string Output17Port = "RngOutput17";

    /// <summary>
    /// Output port 18 name
    /// </summary>
    [DataField("output18Port")]
    public string Output18Port = "RngOutput18";

    /// <summary>
    /// Output port 19 name
    /// </summary>
    [DataField("output19Port")]
    public string Output19Port = "RngOutput19";

    /// <summary>
    /// Output port 20 name
    /// </summary>
    [DataField("output20Port")]
    public string Output20Port = "RngOutput20";

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

    /// <summary>
    /// Cached state prefix for visual updates
    /// </summary>
    public string StatePrefix = string.Empty;

    /// <summary>
    /// Cached array of output ports
    /// </summary>
    public ProtoId<SourcePortPrototype>[]? PortsArray;

    public BoundUserInterface? UserInterface;
}

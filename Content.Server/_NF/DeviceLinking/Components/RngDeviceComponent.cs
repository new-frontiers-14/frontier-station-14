using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

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

    public BoundUserInterface? UserInterface;
}

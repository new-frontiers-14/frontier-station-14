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
    /// Dictionary containing all output ports, indexed by port number (1-20)
    /// </summary>
    [DataField("outputPorts")]
    private Dictionary<int, string> _outputPorts;

    public RngDeviceComponent()
    {
        _outputPorts = new Dictionary<int, string>();
        for (int i = 1; i <= 20; i++)
        {
            _outputPorts[i] = $"RngOutput{i}";
        }
    }

    /// <summary>
    /// Gets the name of the specified output port
    /// </summary>
    /// <param name="portNumber">Port number (1-20)</param>
    /// <returns>The name of the output port</returns>
    public string GetOutputPort(int portNumber)
    {
        if (portNumber < 1 || portNumber > 20)
            throw new ArgumentOutOfRangeException(nameof(portNumber), "Port number must be between 1 and 20");

        return _outputPorts[portNumber];
    }

    /// <summary>
    /// Sets the name of the specified output port
    /// </summary>
    /// <param name="portNumber">Port number (1-20)</param>
    /// <param name="portName">The new name for the port</param>
    public void SetOutputPort(int portNumber, string portName)
    {
        if (portNumber < 1 || portNumber > 20)
            throw new ArgumentOutOfRangeException(nameof(portNumber), "Port number must be between 1 and 20");

        _outputPorts[portNumber] = portName;
    }

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

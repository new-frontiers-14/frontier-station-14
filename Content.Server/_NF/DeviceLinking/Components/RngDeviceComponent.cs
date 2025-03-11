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

    [DataField("inputPort")]
    public ProtoId<SinkPortPrototype> InputPort = "Trigger";

    [DataField("outputPorts")]
    public Dictionary<int, ProtoId<SourcePortPrototype>> OutputPorts = [];

    private static readonly int[] ValidOutputCounts = { 2, 4, 6, 8, 10, 12, 20 };

    public static bool IsValidOutputCount(int outputs)
    {
        return Array.IndexOf(ValidOutputCounts, outputs) >= 0;
    }

    public RngDeviceComponent()
    {
        for (int i = 1; i <= 20; i++)
        {
            OutputPorts[i] = $"RngOutput{i}";
        }

        // Validate Outputs to ensure it's one of the supported values
        if (!IsValidOutputCount(Outputs))
        {
            throw new ArgumentException($"Invalid output count: {Outputs}. Valid values are: {string.Join(", ", ValidOutputCounts)}");
        }
    }

    public ProtoId<SourcePortPrototype> GetOutputPort(int portNumber)
    {
        if (portNumber < 1 || portNumber > 20)
            throw new ArgumentOutOfRangeException(nameof(portNumber), "Port number must be between 1 and 20");

        return OutputPorts[portNumber];
    }

    public void SetOutputPort(int portNumber, ProtoId<SourcePortPrototype> portName)
    {
        if (portNumber < 1 || portNumber > 20)
            throw new ArgumentOutOfRangeException(nameof(portNumber), "Port number must be between 1 and 20");

        OutputPorts[portNumber] = portName;
    }

    // Number of output ports.
    [DataField("outputs")]
    public int Outputs = 6;

    // Initial state
    [DataField]
    public SignalState State = SignalState.Low;

    [DataField("muted")]
    public bool Muted;

    // Target number for percentile dice (1-100). Only used when Outputs = 2.
    [DataField("targetNumber")]
    public int TargetNumber = 50;

    // When enabled, sends High signal to selected port and Low signals to others.
    [DataField("edgeMode")]
    public bool EdgeMode;

    // The last value rolled (1-100 for percentile, 1-N for other dice).
    [DataField("lastRoll")]
    public int LastRoll;

    // The last output port that was triggered (1-based).
    [DataField("lastOutputPort")]
    public int LastOutputPort;

    // Cached state prefix for visual updates
    public string StatePrefix = string.Empty;

    // Cached array of output ports
    public ProtoId<SourcePortPrototype>[]? PortsArray;

    public BoundUserInterface? UserInterface;

    // Gets the device type name based on the number of outputs
    public string GetDeviceType()
    {
        try
        {
            // Get the state prefix and capitalize the first letter
            string prefix = GetStatePrefix();
            return char.ToUpperInvariant(prefix[0]) + prefix.Substring(1);
        }
        catch (ArgumentException)
        {
            // Return "Unknown" for invalid output counts
            return "Unknown";
        }
    }

    public string GetStatePrefix()
    {
        if (!IsValidOutputCount(Outputs))
            throw new ArgumentException($"Unsupported number of outputs: {Outputs}");

        // Special case for percentile
        if (Outputs == 2)
            return "percentile";

        // All other valid outputs are dice (d4, d6, d8, etc.)
        return $"d{Outputs}";
    }
}

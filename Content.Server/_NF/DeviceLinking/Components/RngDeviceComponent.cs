using Content.Server._NF.DeviceLinking.Systems;
using Content.Shared._NF.DeviceLinking;
using Content.Shared._NF.DeviceLinking.Components;
using Robust.Shared.Prototypes;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using SignalState = Content.Shared._NF.DeviceLinking.Components.SignalState;

namespace Content.Server._NF.DeviceLinking.Components;

/// <summary>
/// Frontier: Server-side component for the random number generator device.
/// </summary>
[RegisterComponent, ComponentProtoName("ServerRngDevice"), Access(typeof(RngDeviceSystem))]
public sealed partial class ServerRngDeviceComponent : Component
{
    /// <summary>
    /// The user interface for this device
    /// </summary>
    [DataField("ui")]
    [ViewVariables]
    public BoundUserInterface? UserInterface;

    /// <summary>
    /// Cached array of output ports
    /// </summary>
    [ViewVariables]
    public ProtoId<SourcePortPrototype>[]? PortsArray;

    [DataField("inputPort")]
    public ProtoId<SinkPortPrototype> InputPort = "Trigger";

    [DataField("outputPorts")]
    public Dictionary<int, ProtoId<SourcePortPrototype>> OutputPorts = [];

    private static readonly int[] ValidOutputCounts = { 2, 4, 6, 8, 10, 12, 20 };

    public ServerRngDeviceComponent()
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

    public static bool IsValidOutputCount(int outputs)
    {
        return Array.IndexOf(ValidOutputCounts, outputs) >= 0;
    }

    // Number of output ports.
    [DataField("outputs")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Outputs = 6;

    // Initial state
    [DataField]
    public SignalState State = SignalState.Low;

    [DataField("muted")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Muted;

    // Target number for percentile dice (1-100). Only used when Outputs = 2.
    [DataField("targetNumber")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int TargetNumber = 50;

    // When enabled, sends High signal to selected port and Low signals to others.
    [DataField("edgeMode")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EdgeMode;

    // The last value rolled (1-100 for percentile, 1-N for other dice).
    [DataField("lastRoll")]
    [ViewVariables]
    public int LastRoll;

    // The last output port that was triggered (1-based).
    [DataField("lastOutputPort")]
    [ViewVariables]
    public int LastOutputPort;

    // Cached state prefix for visual updates
    [ViewVariables]
    public string StatePrefix = string.Empty;

    /// <summary>
    /// Gets the device type name based on the number of outputs
    /// </summary>
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

    /// <summary>
    /// Gets the state prefix for the device based on the number of outputs
    /// </summary>
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

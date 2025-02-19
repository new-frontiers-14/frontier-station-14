using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Frontier: A random number generator device that pulses high or low output ports randomly.
/// </summary>
[RegisterComponent, Access(typeof(RngDeviceSystem))]
public sealed partial class RngDeviceComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> InputPort = "RngInput";

    /// <summary>
    /// Name of the rising edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> Output1Port = "RngOutput1";

    /// <summary>
    /// Name of the falling edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> Output2Port = "RngOutput2";

    /// <summary>
    /// Name of the rising edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> Output3Port = "RngOutput3";

    /// <summary>
    /// Name of the falling edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> Output4Port = "RngOutput4";

    /// <summary>
    /// Name of the rising edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> Output5Port = "RngOutput5";

    /// <summary>
    /// Name of the falling edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> Output6Port = "RngOutput6";

    /// <summary>
    /// Number of output ports.
    /// </summary>
    [DataField]
    public int Outputs { get; private set; } = 2;


    // Initial state
    [DataField]
    public SignalState State = SignalState.Low;
}


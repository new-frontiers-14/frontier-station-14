using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Shipyard.Prototypes;

[Prototype]
public sealed class ShuttleAtmospherePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public float Temperature = Atmospherics.T20C;

    [DataField(required: true)]
    public Dictionary<Gas, float> Atmosphere = default!;

    [DataField]
    public ShuttleAtmosphereAlarms? Alarms;

    [DataField]
    public List<Gas>? FilterGases;
}

[DataDefinition]
public sealed partial class ShuttleAtmosphereAlarms
{
    [DataField]
    public ProtoId<AtmosAlarmThresholdPrototype>? TemperatureThresholdId;

    [DataField]
    public AtmosAlarmThreshold? TemperatureThreshold;

    [DataField]
    public ProtoId<AtmosAlarmThresholdPrototype>? PressureThresholdId;

    [DataField]
    public AtmosAlarmThreshold? PressureThreshold;

    [DataField]
    public Dictionary<Gas, ProtoId<AtmosAlarmThresholdPrototype>> GasThresholdPrototypes = new();

    [DataField]
    public Dictionary<Gas, AtmosAlarmThreshold>? GasThresholds;
}

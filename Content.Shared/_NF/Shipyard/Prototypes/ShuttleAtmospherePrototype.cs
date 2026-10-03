using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Shipyard.Prototypes;

public abstract class AtmosphereDefinition
{
    [DataField]
    public float Temperature = Atmospherics.T20C;

    [DataField(required: true)]
    public Dictionary<Gas, float> Atmosphere = default!;
}

[Prototype]
public sealed class ShuttleAtmospherePrototype : AtmosphereDefinition, IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// A set of atmosphere overrides for individual AtmosFixMarker tiles.
    /// See Content.Server.Atmos.EntitySystems.AtmosphereSystem.FixGridAtmosCommand for a list of valid keys.
    /// </summary>
    [DataField]
    public Dictionary<int, ShuttleAtmosphereFixMarkerOverride> AtmosFixOverrides = [];

    [DataField]
    public ShuttleAtmosphereAlarms? Alarms;

    [DataField]
    public List<Gas>? FilterGases;
}

[DataDefinition]
public sealed partial class ShuttleAtmosphereFixMarkerOverride : AtmosphereDefinition;

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

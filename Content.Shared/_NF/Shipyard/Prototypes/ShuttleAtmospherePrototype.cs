using Content.Shared.Atmos;
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
}

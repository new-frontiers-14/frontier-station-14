using Robust.Shared.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

[Prototype]
public sealed partial class GasConversionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    // tuning hinge for if we want a conversion to be more or less productive
    [DataField("baseScaleFactor")]
    public FixedPoint2 BaseScaleFactor = 1.0;

    [DataField("inputGas", required: true)]
    public Gas InputGas;
    [DataField("outputGas", required: true)]
    public Gas OutputGas;
}

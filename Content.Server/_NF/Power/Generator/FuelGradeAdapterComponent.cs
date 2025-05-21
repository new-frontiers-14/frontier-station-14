using Content.Shared.Materials;
using Robust.Shared.Prototypes; // Frontier

namespace Content.Server._NF.Power.Generator;

/// <summary>
/// A component that converts materials at arbitrary rates before inserting into material storage.
/// </summary>
[RegisterComponent]
public sealed partial class FuelGradeAdapterComponent : Component
{
    [DataField(required: true)]
    public List<MaterialAdapterRate> Conversions;
}

[DataDefinition]
public partial record struct MaterialAdapterRate
{
    [DataField(required: true)]
    public ProtoId<MaterialPrototype> Input;

    [DataField(required: true)]
    public ProtoId<MaterialPrototype> Output;

    /// <summary>
    /// The conversion rate - 1 unit of input results in Rate units of output.
    /// </summary>
    [DataField]
    public float Rate = 1.0f;
}

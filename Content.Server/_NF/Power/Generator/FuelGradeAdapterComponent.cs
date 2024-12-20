using Content.Shared.Materials;
using Robust.Shared.Prototypes; // Frontier

namespace Content.Server._NF.Power.Generator;

[RegisterComponent]
public sealed partial class FuelGradeAdapterComponent : Component
{
    [DataField]
    public ProtoId<MaterialPrototype> InputMaterial = "Plasma";

    [DataField]
    public ProtoId<MaterialPrototype> OutputMaterial = "FuelGradePlasma";
}

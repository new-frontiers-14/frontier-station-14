using Content.Server.Cargo.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CashType = "Credit";

    // Frontier
    // The distance in a radius around the console to check for cargo pallets
    // Can be modified individually when mapping, so that consoles have a further reach
    [DataField("palletDistance")]
    public int PalletDistance = 8;
}

using Content.Server._NF.Cargo.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Cargo.Components;

[RegisterComponent]
[Access(typeof(FrontierCargoSystem))]
public sealed partial class FrontierCargoPalletConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CashType = "Credit";
}

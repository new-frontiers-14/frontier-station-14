using Content.Server._NF.Contraband.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Contraband.Components;

[RegisterComponent]
[Access(typeof(ContrabandSystem))]
public sealed partial class ContrabandPalletConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
    public string RewardType = "FrontierUplinkCoin";
}

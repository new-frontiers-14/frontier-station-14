using Content.Server._NF.Contraband.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Contraband.Components; // Extending the shared definition for the server.  Sins.

[RegisterComponent]
[Access(typeof(ContrabandSystem))]
public sealed partial class ContrabandPalletConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", serverOnly: true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
    public string RewardType = "FrontierUplinkCoin";

    [ViewVariables(VVAccess.ReadWrite), DataField(serverOnly: true)]
    public string Faction = "NFSD";
}

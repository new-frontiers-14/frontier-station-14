using Content.Shared._NF.M_Emp;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.MachineLinking;

namespace Content.Shared._NF.M_Emp;

[NetworkedComponent, RegisterComponent]
public sealed class SharedM_EmpComponent : Component
{
    [DataField("receiverPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string ReceiverPort = "M_Emp";
}

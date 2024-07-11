using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Chemistry.Events;


/// <summary>
///     Sends a message to change the associated injector component's ReagentWhitelist to the newReagent
/// </summary>
[Serializable, NetSerializable]
public sealed class ReagentWhitelistChangeMessage : BoundUserInterfaceMessage
{
    public ProtoId<ReagentPrototype> NewReagentProto;

    public ReagentWhitelistChangeMessage(ProtoId<ReagentPrototype> newReagentProto)
    {
        NewReagentProto = newReagentProto;
    }
}

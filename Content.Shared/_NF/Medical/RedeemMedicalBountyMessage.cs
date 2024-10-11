using Robust.Shared.Serialization;

namespace Content.Shared._NF.Medical;

[Serializable, NetSerializable]
public sealed class RedeemMedicalBountyMessage : BoundUserInterfaceMessage
{
    public RedeemMedicalBountyMessage()
    {
    }
}

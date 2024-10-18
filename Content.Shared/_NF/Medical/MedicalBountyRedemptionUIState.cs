using Robust.Shared.Serialization;

namespace Content.Shared._NF.Medical;

[Serializable, NetSerializable]
public enum MedicalBountyRedemptionUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum MedicalBountyRedemptionStatus : byte
{
    NoBody,
    NoBounty,
    TooDamaged,
    NotAlive,
    Valid,
}

[Serializable, NetSerializable]
public enum MedicalBountyRedemptionVisuals : byte
{
    Full
}

[Serializable, NetSerializable]
public sealed class MedicalBountyRedemptionUIState : BoundUserInterfaceState
{
    public int BountyValue { get; }
    public MedicalBountyRedemptionStatus BountyStatus { get; }

    public MedicalBountyRedemptionUIState(MedicalBountyRedemptionStatus bountyStatus, int bountyValue)
    {
        BountyStatus = bountyStatus;
        BountyValue = bountyValue;
    }
}
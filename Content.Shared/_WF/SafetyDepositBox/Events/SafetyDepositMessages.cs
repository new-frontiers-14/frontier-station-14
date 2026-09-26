using Robust.Shared.Serialization;

namespace Content.Shared._WF.SafetyDepositBox.Events;

/// <summary>
/// Message to purchase a new safety deposit box.
/// </summary>
[Serializable, NetSerializable]
public sealed class SafetyDepositPurchaseMessage : BoundUserInterfaceMessage
{
    public SafetyDepositBoxSize BoxSize;

    public SafetyDepositPurchaseMessage(SafetyDepositBoxSize boxSize)
    {
        BoxSize = boxSize;
    }
}

/// <summary>
/// Size options for safety deposit boxes.
/// </summary>
[Serializable, NetSerializable]
public enum SafetyDepositBoxSize
{
    Trial,
    Small,
    Medium,
    Large
}

/// <summary>
/// Message to deposit a box into the console.
/// </summary>
[Serializable, NetSerializable]
public sealed class SafetyDepositDepositMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Message to withdraw a specific box from storage.
/// </summary>
[Serializable, NetSerializable]
public sealed class SafetyDepositWithdrawMessage : BoundUserInterfaceMessage
{
    public Guid BoxId;

    public SafetyDepositWithdrawMessage(Guid boxId)
    {
        BoxId = boxId;
    }
}

/// <summary>
/// Message to reclaim a lost box (delete old record and spawn new empty box).
/// </summary>
[Serializable, NetSerializable]
public sealed class SafetyDepositReclaimMessage : BoundUserInterfaceMessage
{
    public Guid BoxId;

    public SafetyDepositReclaimMessage(Guid boxId)
    {
        BoxId = boxId;
    }
}

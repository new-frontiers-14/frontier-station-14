using Robust.Shared.Serialization;

namespace Content.Shared._WF.SafetyDepositBox.BUI;

/// <summary>
/// State of the safety deposit console UI
/// </summary>
[Serializable, NetSerializable]
public sealed class SafetyDepositConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// List of boxes owned by the current user.
    /// </summary>
    public List<SafetyDepositBoxInfo> OwnedBoxes = new();

    /// <summary>
    /// Amount of cash currently inserted in the console.
    /// </summary>
    public int InsertedCash;

    /// <summary>
    /// Is there a box currently in the box slot?
    /// </summary>
    public bool HasBoxInSlot;

    /// <summary>
    /// Info about the box in the slot, if any.
    /// </summary>
    public SafetyDepositBoxInfo? BoxInSlot;

    /// <summary>
    /// Purchase cost for a trial box.
    /// </summary>
    public int TrialBoxCost;

    /// <summary>
    /// Purchase cost for a small box.
    /// </summary>
    public int SmallBoxCost;

    /// <summary>
    /// Purchase cost for a medium box.
    /// </summary>
    public int MediumBoxCost;

    /// <summary>
    /// Purchase cost for a large box.
    /// </summary>
    public int LargeBoxCost;

    /// <summary>
    /// The current round ID, used to determine if boxes are lost.
    /// </summary>
    public int CurrentRoundId;

    public SafetyDepositConsoleState(
        List<SafetyDepositBoxInfo> ownedBoxes,
        int insertedCash,
        bool hasBoxInSlot,
        SafetyDepositBoxInfo? boxInSlot,
        int trialBoxCost,
        int smallBoxCost,
        int mediumBoxCost,
        int largeBoxCost,
        int currentRoundId)
    {
        OwnedBoxes = ownedBoxes;
        InsertedCash = insertedCash;
        HasBoxInSlot = hasBoxInSlot;
        BoxInSlot = boxInSlot;
        TrialBoxCost = trialBoxCost;
        SmallBoxCost = smallBoxCost;
        MediumBoxCost = mediumBoxCost;
        LargeBoxCost = largeBoxCost;
        CurrentRoundId = currentRoundId;
    }
}

/// <summary>
/// Information about a safety deposit box.
/// </summary>
[Serializable, NetSerializable]
public sealed class SafetyDepositBoxInfo
{
    public Guid BoxId;
    public string OwnerName;
    public bool IsDeposited;
    public string? Nickname;
    public string BoxSize;
    public DateTime? LastWithdrawn;
    public int? LastWithdrawnRoundId;

    public SafetyDepositBoxInfo(Guid boxId, string ownerName, bool isDeposited, string? nickname = null, string boxSize = "Small", DateTime? lastWithdrawn = null, int? lastWithdrawnRoundId = null)
    {
        BoxId = boxId;
        OwnerName = ownerName;
        IsDeposited = isDeposited;
        Nickname = nickname;
        BoxSize = boxSize;
        LastWithdrawn = lastWithdrawn;
        LastWithdrawnRoundId = lastWithdrawnRoundId;
    }
}

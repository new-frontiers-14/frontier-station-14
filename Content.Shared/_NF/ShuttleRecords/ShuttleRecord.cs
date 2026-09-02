using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords;

/**
 * A record of a shuttle that had been purchased.
 * This class is NOT a indication that the shuttle is still in the game, merely a transaction record of it.
 */
[Virtual, NetSerializable, Serializable]
public class ShuttleRecord(
    string name,
    string suffix,
    string ownerName,
    NetEntity entityUid,
    bool purchasedWithVoucher,
    uint purchasePrice,
    string vesselPrototypeId,
    TimeSpan? timeOfPurchase = null,
    TimeSpan? timeOfSale = null
)
{
    [ViewVariables]
    public string Name { get; set; } = name;

    /// <summary>
    /// The ID of the VesselPrototype this shuttle came from. Used internally
    /// for the shipyard statistics printout.
    /// </summary>
    [ViewVariables]
    public string VesselPrototypeId { get; set; } = vesselPrototypeId;

    [ViewVariables]
    public string? Suffix { get; set; } = suffix;

    [ViewVariables]
    public string OwnerName { get; set; } = ownerName;

    /**
     * Entity is deleted when the ship gets sold.
     * Use EntityManager.EntityExists(EntityUid) to check if the entity still exists.
     */
    [ViewVariables]
    public NetEntity EntityUid { get; set; } = entityUid;

    [ViewVariables]
    public TimeSpan? TimeOfPurchase { get; set; } = timeOfPurchase;

    [ViewVariables]
    public TimeSpan? TimeOfSale { get; set; } = timeOfSale;

    // The amount of spesos it had costed to purchase this vessel.
    [ViewVariables]
    public uint PurchasePrice { get; set; } = purchasePrice;

    [ViewVariables]
    public bool PurchasedWithVoucher { get; set; } = purchasedWithVoucher;
}

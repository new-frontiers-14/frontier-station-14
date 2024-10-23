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
    NetEntity entityUid
)
{
    [ViewVariables]
    public string Name { get; set; } = name;

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
}

using Content.Shared.Access.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems;

public abstract class SharedIdCardSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    /// <summary>
    ///     Attempt to find an ID card on an entity. This will look in the entity itself, in the entity's hands, and
    ///     in the entity's inventory.
    /// </summary>
    public bool TryFindIdCard(EntityUid uid, out Entity<IdCardComponent> idCard)
    {
        // check held item?
        if (TryComp(uid, out HandsComponent? hands) &&
            hands.ActiveHandEntity is EntityUid heldItem &&
            TryGetIdCard(heldItem, out idCard))
        {
            return true;
        }

        // check entity itself
        if (TryGetIdCard(uid, out idCard))
            return true;

        // check inventory slot?
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid) && TryGetIdCard(idUid.Value, out idCard))
            return true;

        return false;
    }

    /// <summary>
    ///     Attempt to get an id card component from an entity, either by getting it directly from the entity, or by
    ///     getting the contained id from a <see cref="PdaComponent"/>.
    /// </summary>
    public bool TryGetIdCard(EntityUid uid, out Entity<IdCardComponent> idCard)
    {
        if (TryComp(uid, out IdCardComponent? idCardComp))
        {
            idCard = (uid, idCardComp);
            return true;
        }

        if (TryComp(uid, out PdaComponent? pda)
        && TryComp(pda.ContainedId, out idCardComp))
        {
            idCard = (pda.ContainedId.Value, idCardComp);
            return true;
        }

        idCard = default;
        return false;
    }

    [Serializable, NetSerializable]
    public sealed class WriteToTargetIdMessage : BoundUserInterfaceMessage
    {
        public readonly string FullName;
        public readonly string JobTitle;
        public readonly List<ProtoId<AccessLevelPrototype>> AccessList;
        public readonly string JobPrototype;

        public WriteToTargetIdMessage(string fullName, string jobTitle, List<ProtoId<AccessLevelPrototype>> accessList, string jobPrototype)
        {
            FullName = fullName;
            JobTitle = jobTitle;
            AccessList = accessList;
            JobPrototype = jobPrototype;
        }
    }

    [Serializable, NetSerializable]
    public sealed class WriteToShuttleDeedMessage : BoundUserInterfaceMessage
    {
        public readonly string ShuttleName;
        public readonly string ShuttleSuffix;

        public WriteToShuttleDeedMessage(string shuttleName, string shuttleSuffix)
        {
            ShuttleName = shuttleName;
            ShuttleSuffix = shuttleSuffix;
        }
    }
}

using Content.Shared.CartridgeLoader;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.BountyContracts;

[Serializable, NetSerializable]
public enum BountyContractCategory : byte
{
    Criminal,
    Vacancy,
    Construction,
    Service,
    Other
}

[Serializable, NetSerializable]
public struct BountyContractCategoryMeta
{
    public string Name;
    public Color UiColor;
    public LocId? Announcement;
}

[NetSerializable, Serializable]
public struct BountyContractTargetInfo
{
    public string Name;
    public string? DNA;

    public bool Equals(BountyContractTargetInfo other)
    {
        return DNA == other.DNA;
    }

    public override bool Equals(object? obj)
    {
        return obj is BountyContractTargetInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return DNA != null ? DNA.GetHashCode() : 0;
    }
}

[NetSerializable, Serializable]
public struct BountyContractRequest
{
    public ProtoId<BountyContractCollectionPrototype> Collection;
    public BountyContractCategory Category;
    public string Name;
    public string? DNA;
    public string Vessel;
    public int Reward;
    public string Description;
}

[NetSerializable, Serializable]
public sealed class BountyContract
{
    public readonly uint ContractId;
    public readonly BountyContractCategory Category;
    public readonly string Name;
    public readonly int Reward;
    public readonly NetEntity AuthorUid;
    public readonly string? DNA;
    public readonly string? Vessel;
    public readonly string? Description;
    public readonly string? Author;

    public BountyContract(uint contractId, BountyContractCategory category, string name,
        int reward, NetEntity authorUid, string? dna, string? vessel, string? description, string? author)
    {
        ContractId = contractId;
        Category = category;
        Name = name;
        Reward = reward;
        AuthorUid = authorUid;
        DNA = dna;
        Vessel = vessel;
        Description = description;
        Author = author;
    }
}

[NetSerializable, Serializable]
public sealed class BountyContractCreateUiState : BoundUserInterfaceState
{
    public readonly ProtoId<BountyContractCollectionPrototype> Collection;
    public readonly List<BountyContractTargetInfo> Targets;
    public readonly List<string> Vessels;

    public BountyContractCreateUiState(
        ProtoId<BountyContractCollectionPrototype> collection,
        List<BountyContractTargetInfo> targets,
        List<string> vessels)
    {
        Collection = collection;
        Targets = targets;
        Vessels = vessels;
    }
}

[NetSerializable, Serializable]
public sealed class BountyContractListUiState(ProtoId<BountyContractCollectionPrototype> collection,
        List<ProtoId<BountyContractCollectionPrototype>> collections,
        List<BountyContract> contracts,
        bool isAllowedCreateBounties,
        bool isAllowedRemoveBounties,
        NetEntity authorUid,
        bool notificationsEnabled) : BoundUserInterfaceState
{
    public readonly ProtoId<BountyContractCollectionPrototype> Collection = collection;
    public readonly List<ProtoId<BountyContractCollectionPrototype>> Collections = collections;
    public readonly List<BountyContract> Contracts = contracts;
    public readonly bool IsAllowedCreateBounties = isAllowedCreateBounties;
    public readonly bool IsAllowedRemoveBounties = isAllowedRemoveBounties;
    public readonly NetEntity AuthorUid = authorUid;
    public readonly bool NotificationsEnabled = notificationsEnabled;
}

public enum BountyContractCommand : byte
{
    OpenCreateUi = 0,
    CloseCreateUi = 1,
    RefreshList = 2,
    ToggleNotifications = 3,
}

[NetSerializable, Serializable]
public sealed class BountyContractCommandMessageEvent(BountyContractCommand command, ProtoId<BountyContractCollectionPrototype> collection) : CartridgeMessageEvent
{
    public readonly ProtoId<BountyContractCollectionPrototype> Collection = collection;
    public readonly BountyContractCommand Command = command;
}

[NetSerializable, Serializable]
public sealed class BountyContractTryRemoveMessageEvent(uint contractId) : CartridgeMessageEvent
{
    public readonly uint ContractId = contractId;
}

[NetSerializable, Serializable]
public sealed class BountyContractTryCreateMessageEvent(BountyContractRequest contract) : CartridgeMessageEvent
{
    public readonly BountyContractRequest Contract = contract;
}

public abstract class SharedBountyContractSystem : EntitySystem
{
    public const int MaxNameLength = 32;
    public const int MaxVesselLength = 32;
    public const int MaxDescriptionLength = 256;
    public const int DefaultReward = 5000;

    // TODO: move this to prototypes?
    public static readonly Dictionary<BountyContractCategory, BountyContractCategoryMeta> CategoriesMeta = new()
    {
        [BountyContractCategory.Criminal] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-criminal",
            UiColor = Color.FromHex("#520c0c"),
            Announcement = "bounty-contracts-announcement-criminal-create"
        },
        [BountyContractCategory.Vacancy] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-vacancy",
            UiColor = Color.FromHex("#003866"),
            Announcement = "bounty-contracts-announcement-vacancy-create"
        },
        [BountyContractCategory.Construction] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-construction",
            UiColor = Color.FromHex("#664a06"),
            Announcement = "bounty-contracts-announcement-construction-create"
        },
        [BountyContractCategory.Service] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-service",
            UiColor = Color.FromHex("#01551e"),
            Announcement = "bounty-contracts-announcement-service-create"
        },
        [BountyContractCategory.Other] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-other",
            UiColor = Color.FromHex("#3c3c3c"),
            Announcement = "bounty-contracts-announcement-generic-create"
        },
    };
}

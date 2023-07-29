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
    public readonly string? DNA;
    public readonly string? Vessel;
    public readonly string? Description;
    public readonly string? Author;

    public BountyContract(uint contractId, BountyContractCategory category, string name,
        int reward, string? dna, string? vessel, string? description, string? author)
    {
        ContractId = contractId;
        Category = category;
        Name = name;
        Reward = reward;
        DNA = dna;
        Vessel = vessel;
        Description = description;
        Author = author;
    }
}

[NetSerializable, Serializable]
public sealed class BountyContractCreateUiState : BoundUserInterfaceState
{
    public readonly List<BountyContractTargetInfo> Targets;
    public readonly List<string> Vessels;

    public BountyContractCreateUiState(
        List<BountyContractTargetInfo> targets,
        List<string> vessels)
    {
        Targets = targets;
        Vessels = vessels;
    }
}

[NetSerializable, Serializable]
public sealed class BountyContractListUiState : BoundUserInterfaceState
{
    public readonly List<BountyContract> Contracts;
    public readonly bool IsAllowedCreateBounties;
    public readonly bool IsAllowedRemoveBounties;

    public BountyContractListUiState(List<BountyContract> contracts,
        bool isAllowedCreateBounties, bool isAllowedRemoveBounties)
    {
        Contracts = contracts;
        IsAllowedCreateBounties = isAllowedCreateBounties;
        IsAllowedRemoveBounties = isAllowedRemoveBounties;
    }

}

[NetSerializable, Serializable]
public sealed class BountyContractOpenCreateUiMsg : BoundUserInterfaceMessage
{
}

[NetSerializable, Serializable]
public sealed class BountyContractRefreshListUiMsg : BoundUserInterfaceMessage
{
}

[NetSerializable, Serializable]
public sealed class BountyContractCloseCreateUiMsg : BoundUserInterfaceMessage
{
}

[NetSerializable, Serializable]
public sealed class BountyContractTryRemoveUiMsg : BoundUserInterfaceMessage
{
    public readonly uint ContractId;

    public BountyContractTryRemoveUiMsg(uint contractId)
    {
        ContractId = contractId;
    }
}

[NetSerializable, Serializable]
public sealed class BountyContractTryCreateMsg : BoundUserInterfaceMessage
{
    public readonly BountyContractRequest Contract;

    public BountyContractTryCreateMsg(BountyContractRequest contract)
    {
        Contract = contract;
    }
}

public abstract class SharedBountyContractSystem : EntitySystem
{
    public const int DefaultReward = 5000;

    // TODO: move this to prototypes?
    public static readonly Dictionary<BountyContractCategory, BountyContractCategoryMeta> CategoriesMeta = new()
    {
        [BountyContractCategory.Criminal] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-criminal",
            UiColor = Color.FromHex("#520c0c")
        },
        [BountyContractCategory.Vacancy] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-vacancy",
            UiColor = Color.FromHex("#003866")
        },
        [BountyContractCategory.Construction] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-construction",
            UiColor = Color.FromHex("#664a06")
        },
        [BountyContractCategory.Service] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-service",
            UiColor = Color.FromHex("#01551e")
        },
        [BountyContractCategory.Other] = new BountyContractCategoryMeta
        {
            Name = "bounty-contracts-category-other",
            UiColor = Color.FromHex("#474747")
        },
    };
}

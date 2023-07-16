using Robust.Shared.Serialization;

namespace Content.Shared.StationBounties;

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
public struct BountyContractCreateRequest
{
    public string Name;
    public string? DNA;
    public string Vesel;
    public int Reward;
    public string Description;
}

[NetSerializable, Serializable]
public struct BountyContract
{
    public string Name;
    public string? DNA;
    public string Vesel;
    public int Reward;
    public string Description;
}

[NetSerializable, Serializable]
public enum BountyContractFragmentState : byte
{
    List,
    Create
}

[NetSerializable, Serializable]
public sealed class BountyContractBoundUserInterfaceState : BoundUserInterfaceState
{
    public BountyContractFragmentState State;
}

[NetSerializable, Serializable]
public sealed class BountyContractCreateUiState : BoundUserInterfaceState
{
    public List<BountyContractTargetInfo> Targets;
    public List<string> Vessels;

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
    public List<BountyContract> Contracts;
    public BountyContractListUiState(List<BountyContract> contracts)
    {
        Contracts = contracts;
    }
}



[NetSerializable, Serializable]
public enum StationBountyUiKey : byte
{
    Key
}

public sealed class SharedBountyContractSystem : EntitySystem
{
    // TODO: Cvar?
    public const int MinimalReward = 10000;
}

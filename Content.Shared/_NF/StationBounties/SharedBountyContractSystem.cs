using Robust.Shared.Serialization;

namespace Content.Shared.StationBounties;

[NetSerializable, Serializable]
public struct BountyContractTargetInfo
{
    public string Name;
    public string? DNA;
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
public sealed class BountyContractCreateBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<BountyContractTargetInfo> Targets;
    public List<string> Vessels;

    public BountyContractCreateBoundUserInterfaceState(
        List<BountyContractTargetInfo> targets,
        List<string> vessels)
    {
        Targets = targets;
        Vessels = vessels;
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

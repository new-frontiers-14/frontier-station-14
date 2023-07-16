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
public enum StationBountyUiKey : byte
{
    Key
}

public sealed class SharedBountyContractSystem : EntitySystem
{
    // TODO: Cvar?
    public const int MinimalReward = 10000;
}

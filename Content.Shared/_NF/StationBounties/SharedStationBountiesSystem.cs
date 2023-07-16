using Robust.Shared.Serialization;

namespace Content.Shared.StationBounties;

[NetSerializable, Serializable]
public struct PossibleTargetInfo
{
    public string Name;
    public string? DNA;
}

[NetSerializable, Serializable]
public struct BountyContractInfo
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

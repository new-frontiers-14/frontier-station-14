using Robust.Shared.Serialization;

namespace Content.Shared.StationBounties;

public struct PossibleTargetInfo
{
    public string Name;
    public string? DNA;
}

[NetSerializable, Serializable]
public enum StationBountyUiKey : byte
{
    Key
}

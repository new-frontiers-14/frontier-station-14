using Robust.Shared.Serialization;

namespace Content.Shared._NF.Skrungler;

[Serializable, NetSerializable]
public enum SkrunglerContents : byte
{
    Empty,
    HasMob,
    HasSoul,
    HasContents,
}

[Serializable, NetSerializable]
public enum SkrunglerVisuals : byte
{
    SkrunglingBase,
    Skrungling,
}

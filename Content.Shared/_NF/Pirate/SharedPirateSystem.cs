using Robust.Shared.Serialization;

namespace Content.Shared._NF.Pirate;

[NetSerializable, Serializable]
public enum PirateConsoleUiKey : byte
{
    Bounty,
    BountyRedemption
}

[NetSerializable, Serializable]
public enum PiratePalletConsoleUiKey : byte
{
    Sale
}

public abstract class SharedPirateSystem : EntitySystem {}

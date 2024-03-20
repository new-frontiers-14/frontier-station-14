using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo;

public abstract class SharedFrontierCargoSystem : EntitySystem;

[NetSerializable, Serializable]
public enum FrontierCargoConsoleUiKey : byte
{
    Orders,
    Bounty,
    Shuttle,
    Telepad
}

[NetSerializable, Serializable]
public enum FrontierCargoPalletConsoleUiKey : byte
{
    Sale
}

[Serializable, NetSerializable]
public enum FrontierCargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
}

[Serializable, NetSerializable]
public enum FrontierCargoTelepadVisuals : byte
{
    State,
}

using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Cargo;

public abstract class SharedNFCargoSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}

[NetSerializable, Serializable]
public enum NFCargoConsoleUiKey : byte
{
    Orders,
    Bounty, // FRONTIER MERGE: unused?
    Telepad
}

[NetSerializable, Serializable]
public enum NFCargoPalletConsoleUiKey : byte
{
    Sale
}

[Serializable, NetSerializable]
public enum NFCargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
};

[Serializable, NetSerializable]
public enum NFCargoTelepadVisuals : byte
{
    State,
};

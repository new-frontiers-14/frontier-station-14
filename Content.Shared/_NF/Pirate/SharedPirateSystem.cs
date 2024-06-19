using Robust.Shared.Serialization;

namespace Content.Shared._NF.Pirate;

[NetSerializable, Serializable]
public enum PirateConsoleUiKey : byte
{
    Bounty,
    Telepad
}

[NetSerializable, Serializable]
public enum PiratePalletConsoleUiKey : byte
{
    Sale
}

public abstract class SharedPirateSystem : EntitySystem {}

// TODO: remove these.
// [Serializable, NetSerializable]
// public enum PirateTelepadState : byte
// {
//     Unpowered,
//     Idle,
//     Teleporting,
// };

// [Serializable, NetSerializable]
// public enum PirateTelepadVisuals : byte
// {
//     State,
// };

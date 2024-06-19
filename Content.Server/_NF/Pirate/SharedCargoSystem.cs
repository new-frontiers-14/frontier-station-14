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

// TODO: remove these.
// public abstract class SharedPirateSystem : EntitySystem {}

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

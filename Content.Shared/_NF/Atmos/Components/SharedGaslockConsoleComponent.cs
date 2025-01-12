using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.Components;

/// <summary>
/// Remotely controls gaslocks, their pressures and direction.
/// </summary>
public abstract partial class SharedGaslockConsoleComponent : Component;

[Serializable, NetSerializable]
public enum GaslockConsoleUiKey : byte
{
    Key,
}

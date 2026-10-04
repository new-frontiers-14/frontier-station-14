using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class IFFConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public IFFFlags AllowedFlags;
    public IFFFlags Flags;
    public float HeatCapacity; //Frontier
    public float CurrentHeat; //Frontier
}

[Serializable, NetSerializable]
public enum IFFConsoleUiKey : byte
{
    Key,
}

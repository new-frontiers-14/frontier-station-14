using Robust.Shared.Serialization;

namespace Content.Shared._NF.CrateMachine;

[Serializable, NetSerializable]
public enum CrateMachineVisualState : byte
{
    Open,
    Closed,
    Opening,
    Closing,
}

[Serializable, NetSerializable]
public enum CrateMachineVisuals : byte
{
    VisualState,
}

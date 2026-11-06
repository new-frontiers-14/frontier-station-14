using Robust.Shared.Serialization;

namespace Content.Shared._NF.Power;

/// <summary>
/// UI key for an object with adjustable power draw.
/// </summary>
[NetSerializable, Serializable]
public enum AdjustablePowerDrawUiKey : byte
{
    Key,
}

/// <summary>
/// UI state for a machine with adjustable power draw.
/// </summary>
/// <seealso cref="AdjustablePowerDrawUiKey"/>
[Serializable, NetSerializable]
public sealed class AdjustablePowerDrawBuiState : BoundUserInterfaceState
{
    public bool On;
    public float Load;
    public string? Text;
}

/// <summary>
/// Sent client to server to change the input breaker state on a large battery.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdjustablePowerDrawSetEnabledMessage(bool on) : BoundUserInterfaceMessage
{
    public bool On = on;
}

/// <summary>
/// Sent client to server to change the input breaker state on a large battery.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdjustablePowerDrawSetLoadMessage(float load) : BoundUserInterfaceMessage
{
    public float Load = load;
}

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Implants.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwanSongImplantComponent : Component
{
    [DataField]
    public TimeSpan TriggerCooldown = TimeSpan.FromMinutes(1);

    [DataField]
    [AutoNetworkedField]
    public string Message = "I'm down! Please come get me!";

    [DataField]
    [AutoNetworkedField]
    public SwanSongOutputMode OutputMode = SwanSongOutputMode.Local;

    [ViewVariables]
    public TimeSpan? LastTriggerTime;
}

public sealed partial class DistressImplantOpenMenuEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum SwanSongOutputMode : byte
{
    Local,
    Common,
    Medical
}

[Serializable, NetSerializable]
public enum DistressImplantUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DistressImplantBuiState(string message, SwanSongOutputMode mode) : BoundUserInterfaceState
{
    public string Message { get; } = message;
    public SwanSongOutputMode Mode { get; } = mode;
}

[Serializable, NetSerializable]
public sealed class DistressImplantSetModeMessage(SwanSongOutputMode mode) : BoundUserInterfaceMessage
{
    public SwanSongOutputMode Mode { get; } = mode;
}

[Serializable, NetSerializable]
public sealed class DistressImplantSetMessageMessage(string message) : BoundUserInterfaceMessage
{
    public string Message { get; } = message;
}

using Robust.Shared.Serialization;

namespace Content.Shared.MachineLinking;

[Serializable, NetSerializable]
public enum SignalTimerUiKey : byte
{
    Key
}

/// <summary>
/// Represents a SignalTimerComponent state that can be sent to the client
/// </summary>
[Serializable, NetSerializable]
public sealed class SignalTimerBoundUserInterfaceState : BoundUserInterfaceState
{
    public string CurrentText;
    public string CurrentDelayMinutes;
    public string CurrentDelaySeconds;
    public bool CurrentRepeat; //Frontier
    public bool ShowText;
    public TimeSpan TriggerTime;
    public bool TimerStarted;
    public bool HasAccess;

    public SignalTimerBoundUserInterfaceState(string currentText,
        string currentDelayMinutes,
        string currentDelaySeconds,
        bool currentRepeat, //Frontier
        bool showText,
        TimeSpan triggerTime,
        bool timerStarted,
        bool hasAccess)
    {
        CurrentText = currentText;
        CurrentDelayMinutes = currentDelayMinutes;
        CurrentDelaySeconds = currentDelaySeconds;
        CurrentRepeat = currentRepeat; //Frontier
        ShowText = showText;
        TriggerTime = triggerTime;
        TimerStarted = timerStarted;
        HasAccess = hasAccess;
    }
}

[Serializable, NetSerializable]
public sealed class SignalTimerTextChangedMessage : BoundUserInterfaceMessage
{
    public string Text { get; }

    public SignalTimerTextChangedMessage(string text)
    {
        Text = text;
    }
}

//Frontier: SignalTimerRepeatToggled class
[Serializable, NetSerializable]
public sealed class SignalTimerRepeatToggled : BoundUserInterfaceMessage
{
    public bool Repeat { get; }

    public SignalTimerRepeatToggled(bool repeat)
    {
        Repeat = repeat;
    }
}
//End Frontier

[Serializable, NetSerializable]
public sealed class SignalTimerDelayChangedMessage : BoundUserInterfaceMessage
{
    public TimeSpan Delay { get; }
    public SignalTimerDelayChangedMessage(TimeSpan delay)
    {
        Delay = delay;
    }
}

[Serializable, NetSerializable]
public sealed class SignalTimerStartMessage : BoundUserInterfaceMessage
{

}

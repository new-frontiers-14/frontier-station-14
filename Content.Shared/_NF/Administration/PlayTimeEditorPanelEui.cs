using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Administration;

[Serializable, NetSerializable]
public sealed class PlayTimeEditorPanelEuiState : EuiStateBase
{
    public bool HasFlag { get; }

    public PlayTimeEditorPanelEuiState(bool hasFlag)
    {
        HasFlag = hasFlag;
    }
}

[Serializable, NetSerializable]
public sealed class PlayTimeEditorEuiMessage : EuiMessageBase
{
    public string PlayerId { get; }
    public List<PlayTimeEditorData> TimeData { get; }

    public bool Overwrite { get; }

    public PlayTimeEditorEuiMessage(string playerId, List<PlayTimeEditorData> timeData, bool overwrite)
    {
        PlayerId = playerId;
        TimeData = timeData;
        Overwrite = overwrite;
    }
}

[Serializable, NetSerializable]
public sealed class PlayTimeEditorWarningEuiMessage : EuiMessageBase
{
    public string Message { get; }
    public Color WarningColor { get; }

    public PlayTimeEditorWarningEuiMessage(string message, Color color)
    {
        Message = message;
        WarningColor = color;
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public partial record struct PlayTimeEditorData
{
    [DataField]
    public string TimeString { get; init; }

    [DataField]
    public string PlaytimeTracker { get; init; }

    public PlayTimeEditorData(string tracker, string timeString)
    {
        PlaytimeTracker = tracker;
        TimeString = timeString;
    }
}

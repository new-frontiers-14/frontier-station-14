using Content.Client.Eui;
using Content.Shared._NF.Administration;
using Content.Shared.Eui;

namespace Content.Client._NF.Administration.UI.PlayTimeEditorPanel;

public sealed class PlayTimeEditorPanelEui : BaseEui
{
    public PlayTimeEditorPanel PlayTimeEditorPanel { get; }

    public PlayTimeEditorPanelEui()
    {
        PlayTimeEditorPanel = new PlayTimeEditorPanel();
        PlayTimeEditorPanel.OnPlaytimeMessageSend += args => SendMessage(new PlayTimeEditorEuiMessage(args.playerId, args.playtimeList, args.overwrite));
    }

    public override void Opened()
    {
        PlayTimeEditorPanel.OpenCentered();
    }

    public override void Closed()
    {
        PlayTimeEditorPanel.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PlayTimeEditorPanelEuiState cast)
            return;

        PlayTimeEditorPanel.UpdateFlag(cast.HasFlag);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PlayTimeEditorWarningEuiMessage warning)
            return;

        PlayTimeEditorPanel.UpdateWarning(warning.Message, warning.WarningColor);
    }
}

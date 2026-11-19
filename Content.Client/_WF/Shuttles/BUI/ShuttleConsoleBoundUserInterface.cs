using Content.Client.Shuttles.UI;
using Content.Shared._WF.Shuttles.Events;

namespace Content.Client.Shuttles.BUI;

public sealed partial class ShuttleConsoleBoundUserInterface
{
    private void WfOpen()
    {
        _window ??= new ShuttleConsoleWindow();
        _window.OnAutopilotToggled += OnAutopilotToggled;
    }

    private void OnAutopilotToggled(NetEntity? entityUid)
    {
        SendMessage(new ToggleAutopilotRequest
        {
            ShuttleEntityUid = entityUid,
        });
    }
}

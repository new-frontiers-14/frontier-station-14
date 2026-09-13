namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleConsoleWindow
{
    public event Action<NetEntity?>? OnAutopilotToggled;

    private void WfInitialize()
    {
        NavContainer.OnAutopilotToggled += (entity) =>
        {
            OnAutopilotToggled?.Invoke(entity);
        };
    }
}

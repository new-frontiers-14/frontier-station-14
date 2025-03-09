using Content.Client.Eui;
using Content.Shared._DV.Administration;
using Content.Shared.Eui;

namespace Content.Client._DV.Administration.UI;

public sealed class JobWhitelistsEui : BaseEui
{
    private JobWhitelistsWindow Window;

    public JobWhitelistsEui()
    {
        Window = new JobWhitelistsWindow();
        Window.OnClose += () => SendMessage(new CloseEuiMessage());
        Window.OnSetJob += (id, whitelisted) => SendMessage(new SetJobWhitelistedMessage(id, whitelisted));
        Window.OnSetGhostRole += (id, whitelisted) => SendMessage(new SetGhostRoleWhitelistedMessage(id, whitelisted)); // Frontier
        Window.OnSetGlobal += (whitelisted) => SendMessage(new SetGlobalWhitelistMessage(whitelisted)); // Frontier
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not JobWhitelistsEuiState cast)
            return;

        Window.HandleState(cast);
    }

    public override void Opened()
    {
        base.Opened();

        Window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        Window.Close();
        Window.Dispose();
    }
}

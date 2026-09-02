using Content.Client.Eui;
using Content.Shared._DV.Administration;
using Content.Shared.Eui;

namespace Content.Client._DV.Administration.UI;

public sealed class JobWhitelistsEui : BaseEui
{
    private readonly JobWhitelistsWindow _window;

    public JobWhitelistsEui()
    {
        _window = new JobWhitelistsWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnSetJob += (id, whitelisted) => SendMessage(new SetJobWhitelistedMessage(id, whitelisted));
        _window.OnSetGhostRole += (id, whitelisted) => SendMessage(new SetGhostRoleWhitelistedMessage(id, whitelisted)); // Frontier
        _window.OnSetGlobal += (whitelisted) => SendMessage(new SetGlobalWhitelistMessage(whitelisted)); // Frontier
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not JobWhitelistsEuiState cast)
            return;

        _window.HandleState(cast);
    }

    public override void Opened()
    {
        base.Opened();

        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _window.Close();
    }
}

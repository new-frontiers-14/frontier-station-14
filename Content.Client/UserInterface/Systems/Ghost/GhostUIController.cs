using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared.CCVar;
using Content.Shared._NF.CCVar; // Frontier
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.IoC;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;
using Content.Server.Corvax.Respawn;

namespace Content.Client.UserInterface.Systems.Ghost;

// TODO hud refactor BEFORE MERGE fix ghost gui being too far up
public sealed class GhostUIController : UIController, IOnSystemChanged<GhostSystem>, IOnSystemChanged<RespawnSystem>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [UISystemDependency] private readonly GhostSystem? _system = default;
    [UISystemDependency] private readonly RespawnSystem? _respawn = default;

    private GhostGui? Gui => UIManager.GetActiveUIWidgetOrNull<GhostGui>();
    private bool _canUncryo = true; // Frontier. TODO: find a reliable way to update this, for now it just stays active all the time

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        LoadGui();
    }

    private void OnScreenUnload()
    {
        UnloadGui();
    }

    public void OnSystemLoaded(GhostSystem system)
    {
        system.PlayerRemoved += OnPlayerRemoved;
        system.PlayerUpdated += OnPlayerUpdated;
        system.PlayerAttached += OnPlayerAttached;
        system.PlayerDetached += OnPlayerDetached;
        system.GhostWarpsResponse += OnWarpsResponse;
        system.GhostRoleCountUpdated += OnRoleCountUpdated;
    }

    public void OnSystemUnloaded(GhostSystem system)
    {
        system.PlayerRemoved -= OnPlayerRemoved;
        system.PlayerUpdated -= OnPlayerUpdated;
        system.PlayerAttached -= OnPlayerAttached;
        system.PlayerDetached -= OnPlayerDetached;
        system.GhostWarpsResponse -= OnWarpsResponse;
        system.GhostRoleCountUpdated -= OnRoleCountUpdated;
    }

    public void OnSystemLoaded(RespawnSystem system)
    {
        system.RespawnReseted += OnRespawnReseted;
    }

    public void OnSystemUnloaded(RespawnSystem system)
    {
        system.RespawnReseted -= OnRespawnReseted;
    }

    private void OnRespawnReseted()
    {
        UpdateGui();
        UpdateRespawn(_respawn?.RespawnResetTime);
    }

    public void UpdateGui()
    {
        if (Gui == null)
        {
            return;
        }

        Gui.Visible = _system?.IsGhost ?? false;
        Gui.Update(_system?.AvailableGhostRoleCount, _system?.Player?.CanReturnToBody,
            _canUncryo && _cfg.GetCVar(NFCCVars.CryoReturnEnabled));
    }

    private void UpdateRespawn(TimeSpan? timeOfDeath)
    {
        Gui?.UpdateRespawn(timeOfDeath);
    }

    private void OnPlayerRemoved(GhostComponent component)
    {
        Gui?.Hide();
//        UpdateRespawn(component.TimeOfDeath);
    }

    private void OnPlayerUpdated(GhostComponent component)
    {
//        UpdateRespawn(component.TimeOfDeath);
        UpdateGui();
    }

    private void OnPlayerAttached(GhostComponent component)
    {
        if (Gui == null)
            return;

        Gui.Visible = true;
        UpdateRespawn(_respawn?.RespawnResetTime);
        UpdateGui();
    }

    private void OnPlayerDetached()
    {
        Gui?.Hide();
    }

    private void OnWarpsResponse(GhostWarpsResponseEvent msg)
    {
        if (Gui?.TargetWindow is not { } window)
            return;

        window.UpdateWarps(msg.Warps);
        window.Populate();
    }

    private void OnRoleCountUpdated(GhostUpdateGhostRoleCountEvent msg)
    {
        UpdateGui();
    }

    private void OnWarpClicked(NetEntity player)
    {
        var msg = new GhostWarpToTargetRequestEvent(player);
        _net.SendSystemNetworkMessage(msg);
    }

    private void OnGhostnadoClicked()
    {
        var msg = new GhostnadoRequestEvent();
        _net.SendSystemNetworkMessage(msg);
    }

    public void LoadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed += RequestWarps;
        Gui.ReturnToBodyPressed += ReturnToBody;
        Gui.GhostRolesPressed += GhostRolesPressed;
        Gui.TargetWindow.WarpClicked += OnWarpClicked;
        Gui.TargetWindow.OnGhostnadoClicked += OnGhostnadoClicked;
        Gui.GhostRespawnPressed += GuiOnGhostRespawnPressed;
        UpdateGui();
    }

    private void GuiOnGhostRespawnPressed()
    {
        _consoleHost.ExecuteCommand("ghostrespawn");
    }

    public void UnloadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed -= RequestWarps;
        Gui.ReturnToBodyPressed -= ReturnToBody;
        Gui.GhostRolesPressed -= GhostRolesPressed;
        Gui.TargetWindow.WarpClicked -= OnWarpClicked;
        Gui.GhostRespawnPressed -= GuiOnGhostRespawnPressed;

        Gui.Hide();
    }

    private void ReturnToBody()
    {
        _system?.ReturnToBody();
    }

    private void RequestWarps()
    {
        _system?.RequestWarps();
        Gui?.TargetWindow.Populate();
        Gui?.TargetWindow.OpenCentered();
    }

    private void GhostRolesPressed()
    {
        _system?.OpenGhostRoles();
    }

    private void RespawnPressed()
    {
        IoCManager.Resolve<IClientConsoleHost>().RemoteExecuteCommand(null, "ghostrespawn");
    }
}

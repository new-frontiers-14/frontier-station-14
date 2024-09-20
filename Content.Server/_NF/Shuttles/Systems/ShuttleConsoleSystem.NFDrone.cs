using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.UserInterface;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleConsoleSystem
{
    public void InitializeNFDrone()
    {
        SubscribeLocalEvent<NFDroneConsoleComponent, ConsoleShuttleEvent>(OnNFCargoGetConsole);
        SubscribeLocalEvent<NFDroneConsoleComponent, AfterActivatableUIOpenEvent>(OnNFDronePilotConsoleOpen);
        Subs.BuiEvents<NFDroneConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnNFDronePilotConsoleClose);
        });
    }

    /// <summary>
    /// Gets the drone console target if applicable otherwise returns itself.
    /// </summary>
    public EntityUid? GetNFDroneConsole(EntityUid consoleUid)
    {
        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = consoleUid,
        };

        RaiseLocalEvent(consoleUid, ref getShuttleEv);
        return getShuttleEv.Console;
    }

    private void OnNFDronePilotConsoleOpen(EntityUid uid, NFDroneConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        component.Entity = GetNFShuttleConsole(uid);
    }

    private void OnNFDronePilotConsoleClose(EntityUid uid, NFDroneConsoleComponent component, BoundUIClosedEvent args)
    {
        // Only if last person closed UI.
        if (!_ui.IsUiOpen(uid, args.UiKey))
            component.Entity = null;
    }

    private void OnNFCargoGetConsole(EntityUid uid, NFDroneConsoleComponent component, ref ConsoleShuttleEvent args)
    {
        args.Console = GetNFShuttleConsole(uid, component);
    }

    /// <summary>
    /// Gets the relevant shuttle console to proxy from the drone console.
    /// </summary>
    private EntityUid? GetNFShuttleConsole(EntityUid uid, NFDroneConsoleComponent? sourceComp = null)
    {
        if (!Resolve(uid, ref sourceComp))
            return null;

        var query = AllEntityQuery<ShuttleConsoleComponent, NFDroneConsoleTargetComponent>();

        while (query.MoveNext(out var cUid, out _, out var targetComp))
        {
            if (sourceComp.Id == targetComp.Id)
                return cUid;
        }

        return null;
    }
}

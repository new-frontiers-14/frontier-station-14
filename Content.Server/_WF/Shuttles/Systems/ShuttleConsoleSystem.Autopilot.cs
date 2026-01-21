using Content.Server._WF.Shuttles.Components;
using Content.Server._WF.Shuttles.Systems;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared._WF.Shuttles.Events;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleConsoleSystem
{
    [Dependency] private readonly AutopilotSystem _autopilot = default!;

    private void InitializeAutopilot()
    {
        SubscribeLocalEvent<ShuttleConsoleComponent, ToggleAutopilotRequest>(OnToggleAutopilotRequest);
    }

    private void OnToggleAutopilotRequest(EntityUid uid, ShuttleConsoleComponent component, ToggleAutopilotRequest args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        // Get the shuttle this console controls
        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = uid,
        };
        RaiseLocalEvent(uid, ref getShuttleEv);
        var shuttleUid = getShuttleEv.Console;

        if (shuttleUid == null)
            return;

        if (!TryComp<TransformComponent>(shuttleUid, out var shuttleXform))
            return;

        var shuttleGridUid = shuttleXform.GridUid;
        if (shuttleGridUid == null)
            return;

        ToggleAutopilot(user, uid, shuttleGridUid.Value);
    }

    private void ToggleAutopilot(EntityUid user, EntityUid consoleUid, EntityUid shuttleUid)
    {
        if (!TryComp<ShuttleConsoleComponent>(consoleUid, out _))
            return;

        if (!TryComp<ShuttleComponent>(shuttleUid, out _))
            return;

        // Check if an autopilot server is installed on the shuttle grid
        if (!HasAutopilotServer(shuttleUid))
        {
            _popup.PopupEntity(Loc.GetString("shuttle-console-autopilot-no-server"), user, user);
            return;
        }

        // Check if autopilot is already enabled - if so, do nothing
        // (autopilot is disabled by selecting another mode, not by clicking the button again)
        var hasAutopilot = TryComp<AutopilotComponent>(shuttleUid, out var autopilotComp);
        var isEnabled = hasAutopilot && autopilotComp!.Enabled;
        if (isEnabled)
            return;

        // Try to get the target from the radar console
        if (!TryComp<RadarConsoleComponent>(consoleUid, out var radarConsoleComponent))
        {
            _popup.PopupEntity(Loc.GetString("shuttle-console-autopilot-no-target"), user, user);
            return;
        }

        // First try to use entity target
        var targetEntity = radarConsoleComponent.TargetEntity;
        if (
            targetEntity != null &&
            targetEntity.Value.IsValid() &&
            TryComp<TransformComponent>(targetEntity.Value, out var targetXform)
        )
        {
            var targetCoords = _transform.GetMapCoordinates(targetXform);
            var destinationName = radarConsoleComponent.TargetEntityName ?? "Unknown Target";
            EnableAutopilot(user, shuttleUid, targetCoords, destinationName);
            return;
        }

        // Otherwise try to use manual coordinate target
        var manualTarget = radarConsoleComponent.Target;
        if (manualTarget != null && TryComp<TransformComponent>(consoleUid, out var consoleXform))
        {
            var targetCoords = new MapCoordinates(manualTarget.Value, consoleXform.MapID);
            EnableAutopilot(user, shuttleUid, targetCoords, "Manual Destination");
            return;
        }

        _popup.PopupEntity(Loc.GetString("shuttle-console-autopilot-no-target"), user, user);
    }

    private void EnableAutopilot(EntityUid user, EntityUid shuttleUid, MapCoordinates targetCoords, string destinationName)
    {
        _autopilot.EnableAutopilot(shuttleUid, targetCoords, destinationName);
        _popup.PopupEntity(Loc.GetString("shuttle-console-autopilot-enabled", ("destination", destinationName)), user, user);
        _autopilot.SendShuttleMessage(shuttleUid, $"Autopilot engaged: Destination: {destinationName}");
    }

    /// <summary>
    /// Gets the autopilot state for a shuttle from a console entity.
    /// </summary>
    public (bool HasServer, bool Enabled) WfGetAutopilotState(EntityUid consoleUid)
    {
        if (!TryComp<TransformComponent>(consoleUid, out var xform) || xform.GridUid == null)
            return (false, false);

        var gridUid = xform.GridUid.Value;
        var hasServer = HasAutopilotServer(gridUid);
        var isEnabled = TryComp<AutopilotComponent>(gridUid, out var autopilot) && autopilot.Enabled;

        return (hasServer, isEnabled);
    }

    /// <summary>
    /// Checks if an autopilot server is installed and powered on the shuttle grid.
    /// </summary>
    private bool HasAutopilotServer(EntityUid shuttleGridUid)
    {
        var query = EntityQueryEnumerator<AutopilotServerComponent, TransformComponent>();
        while (query.MoveNext(out var serverUid, out var server, out var xform))
        {
            // Check if the server is on the same grid as the shuttle
            if (xform.GridUid == shuttleGridUid)
            {
                // Check if the server is anchored and powered
                if (xform.Anchored)
                {
                    // Check if powered (if it has a power receiver, check if it's powered)
                    if (!TryComp<ApcPowerReceiverComponent>(serverUid, out var powerReceiver) || powerReceiver.Powered)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

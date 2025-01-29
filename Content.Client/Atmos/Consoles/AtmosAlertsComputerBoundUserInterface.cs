using Content.Shared.Atmos.Components;
using Content.Shared.Shuttles.Events; // Frontier
using Content.Shared._NF.Atmos.BUI; // Frontier

namespace Content.Client.Atmos.Consoles;

public sealed class AtmosAlertsComputerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AtmosAlertsComputerWindow? _menu;

    public AtmosAlertsComputerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _menu = new AtmosAlertsComputerWindow(this, Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (AtmosAlertsComputerBoundInterfaceState) state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
        _menu?.UpdateUI(xform?.Coordinates, castState.AirAlarms, castState.FireAlarms, castState.FocusData, castState.Gaslocks, castState.FocusGaslockData); // Frontier: add gaslocks, focusGaslockData
    }

    public void SendFocusChangeMessage(NetEntity? netEntity)
    {
        SendMessage(new AtmosAlertsComputerFocusChangeMessage(netEntity));
    }

    public void SendDeviceSilencedMessage(NetEntity netEntity, bool silenceDevice)
    {
        SendMessage(new AtmosAlertsComputerDeviceSilencedMessage(netEntity, silenceDevice));
    }

    // Frontier: gaslock message
    public void SendGaslockChangeDirectionMessage(NetEntity netEntity, bool direction)
    {
        SendMessage(new RemoteGasPressurePumpChangePumpDirectionMessage(netEntity, direction));
    }

    public void SendGaslockPressureChangeMessage(NetEntity netEntity, float pressure)
    {
        SendMessage(new RemoteGasPressurePumpChangeOutputPressureMessage(netEntity, pressure));
    }

    public void SendGaslockChangeEnabled(NetEntity netEntity, bool enabled)
    {
        SendMessage(new RemoteGasPressurePumpToggleStatusMessage(netEntity, enabled));
    }

    public void SendGaslockUndock(NetEntity netEntity)
    {
        SendMessage(new UndockRequestMessage { DockEntity = netEntity });
    }
    // End Frontier

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}

using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Shipyard.Components;

namespace Content.Server.Corvax.FaxMultiaddress;

public sealed class FaxMultiaddressSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _network = default!;
    [Dependency] private readonly FaxSystem _fax = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<FaxMultiaddressComponent, DeviceNetworkPacketEvent>(OnDeviceNetworkPacket);
    }

    private void OnRoundStarted(RoundStartedEvent e)
    {
        Spawn("FaxMultiaddress");
    }

    private void OnDeviceNetworkPacket(EntityUid entity, FaxMultiaddressComponent component, DeviceNetworkPacketEvent e)
    {
        if (!TryComp<DeviceNetworkComponent>(entity, out var device))
            return;

        if (!e.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        switch (command)
        {
            case FaxConstants.FaxPingCommand:
                _network.QueuePacket(entity, e.SenderAddress, new()
                {
                    [DeviceNetworkConstants.Command] = FaxConstants.FaxPongCommand,
                    [FaxConstants.FaxNameData] = "All Shuttles"
                });

                break;
            case FaxConstants.FaxPrintCommand:
                if (!e.Data.TryGetValue(FaxConstants.FaxPaperNameData, out string? name))
                    return;

                if (!e.Data.TryGetValue(FaxConstants.FaxPaperContentData, out string? content))
                    return;

                e.Data.TryGetValue(FaxConstants.FaxPaperLabelData, out string? label);
                e.Data.TryGetValue(FaxConstants.FaxPaperStampStateData, out string? stampState);
                e.Data.TryGetValue(FaxConstants.FaxPaperStampedByData, out List<StampDisplayInfo>? stampedBy);
                e.Data.TryGetValue(FaxConstants.FaxPaperPrototypeData, out string? prototype);

                FaxPrintout printout = new(content, name, label, prototype, stampState, stampedBy);

                var faxes = AllEntityQuery<FaxMachineComponent>();

                while (faxes.MoveNext(out var fax, out var faxComponent))
                    if (fax != e.Sender && HasComp<ShuttleDeedComponent>(Transform(fax).GridUid))
                        _fax.Receive(fax, printout, device.Address, faxComponent);

                break;
        }
    }
}

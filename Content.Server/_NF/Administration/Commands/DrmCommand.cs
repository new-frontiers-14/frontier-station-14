using Content.Server._NF.BindToStation;
using Content.Server.Station.Systems;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Commands;

[ToolshedCommand, AdminCommand(Shared.Administration.AdminFlags.Debug)]
public sealed class DrmCommand : ToolshedCommand
{
    private BindToStationSystem? _bindToStation;
    private StationSystem? _station;

    [CommandImplementation("autobind")]
    public EntityUid Autobind([PipedArgument] EntityUid input)
    {
        _bindToStation ??= GetSys<BindToStationSystem>();
        _station ??= GetSys<StationSystem>();

        if (_station.GetOwningStation(input) is { } station)
        {
            _bindToStation.BindToStation(input, station, enabled: true);
        }

        return input;
    }

    [CommandImplementation("bindto")]
    public EntityUid BindTo([PipedArgument] EntityUid input, EntityUid station)
    {
        _bindToStation ??= GetSys<BindToStationSystem>();
        _bindToStation.BindToStation(input, station, enabled: true);
        return input;
    }

    [CommandImplementation("unbind")]
    public EntityUid Unbind([PipedArgument] EntityUid input)
    {
        _bindToStation ??= GetSys<BindToStationSystem>();
        _bindToStation.BindToStation(input, null, enabled: false);
        return input;
    }
}

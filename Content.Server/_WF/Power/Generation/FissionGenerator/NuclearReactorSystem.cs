using Content.Shared.Station.Components;

using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;


namespace Content.Server._FarHorizons.Power.Generation.FissionGenerator;

public sealed partial class NuclearReactorSystem : SharedNuclearReactorSystem
{
    private string GetReactorLocation(EntityUid uid)
    {
        if (_station.GetOwningStation(uid) is { Valid: true } station
            && TryComp<StationDataComponent>(station, out var stationData)
            && _station.GetLargestGrid((station, stationData)) is { Valid: true } stationGrid
            && TryName(stationGrid, out var gridName)
            && gridName != null)
        {
            return gridName;
        }
        else
        {
            return "Unknown";
        }
    }
}

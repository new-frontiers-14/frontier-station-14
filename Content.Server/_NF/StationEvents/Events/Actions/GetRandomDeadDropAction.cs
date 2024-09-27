using System.Text;
using Content.Server._NF.Smuggling.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// An action that gets a set number of dead drops from a 
/// </summary> 
[DataDefinition]
public sealed partial class GetRandomDeadDropAction : IPreFaxAction
{
    private IEntityManager _entityManager = default!;
    private IRobustRandom _random = default!;
    private StationSystem _station = default!;

    const int MaxHintTimeErrorSeconds = 300;

    public void Initialize()
    {
        _entityManager = IoCManager.Resolve<IEntityManager>();
        _random = IoCManager.Resolve<IRobustRandom>();

        _station = _entityManager.EntitySysManager.GetEntitySystem<StationSystem>();
    }

    public void Format(EntityUid station, ref EditableFaxPrintout printout, ref string? fromAddress)
    {
        List<(EntityUid station, EntityUid ent)> entityList = new();
        var hintQuery = _entityManager.AllEntityQueryEnumerator<DeadDropComponent>();
        while (hintQuery.MoveNext(out var ent, out var _))
        {
            var stationUid = _station.GetOwningStation(ent);
            if (stationUid != null)
                entityList.Add((stationUid.Value, ent));
        }

        _random.Shuffle(entityList);

        int hintCount = _random.Next(2, 4);

        var hintLines = new StringBuilder();
        var hints = 0;
        for (var i = 0; i < entityList.Count && hints < hintCount; i++)
        {
            var hintTuple = entityList[i];
            string objectHintString;
            if (_entityManager.TryGetComponent<PotentialDeadDropComponent>(hintTuple.Item2, out var potentialDeadDrop))
                objectHintString = Loc.GetString(potentialDeadDrop.HintText);
            else
                objectHintString = Loc.GetString("dead-drop-hint-generic");

            string stationHintString;
            if (_entityManager.TryGetComponent(hintTuple.Item1, out MetaDataComponent? stationMetadata))
                stationHintString = stationMetadata.EntityName;
            else
                stationHintString = Loc.GetString("dead-drop-station-hint-generic");

            string timeString;
            if (_entityManager.TryGetComponent<DeadDropComponent>(hintTuple.Item2, out var deadDrop) && deadDrop.NextDrop != null)
            {
                var dropTimeWithError = deadDrop.NextDrop.Value + TimeSpan.FromSeconds(_random.Next(-MaxHintTimeErrorSeconds, MaxHintTimeErrorSeconds));
                timeString = Loc.GetString("dead-drop-time-known", ("time", dropTimeWithError.ToString("hh\\:mm") + ":00"));
            }
            else
            {
                timeString = Loc.GetString("dead-drop-time-unknown");
            }

            hintLines.AppendLine(Loc.GetString("dead-drop-hint-line", ("object", objectHintString), ("poi", stationHintString), ("time", timeString)));
            hints++;
        }
        var hintText = new StringBuilder();
        hintText.AppendLine(Loc.GetString("dead-drop-hint-note", ("drops", hintLines)));

        printout.Content = hintText.ToString();
    }
}

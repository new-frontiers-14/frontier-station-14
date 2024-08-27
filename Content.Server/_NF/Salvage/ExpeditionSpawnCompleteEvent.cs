namespace Content.Server._NF.Salvage;

/// <summary>
///     This event is raised when an expedition spawn job has completed (either successfully or in failure), and informs whether the job was successful or not.
/// </summary>
public sealed class ExpeditionSpawnCompleteEvent : EntityEventArgs
{
    public EntityUid Station;
    public bool Success;
    public ushort MissionIndex;
    public ExpeditionSpawnCompleteEvent(EntityUid station, bool success, ushort missionIndex)
    {
        Station = station;
        Success = success;
        MissionIndex = missionIndex;
    }
}
